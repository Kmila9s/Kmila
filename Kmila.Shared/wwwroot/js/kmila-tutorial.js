/*
 * kmila-tutorial.js — coachmark tour engine.
 *
 * Self-contained DOM engine. No external libraries. Renders a translucent
 * backdrop, a spotlight cut-out around the target element, and a floating
 * card with Back / Next / Skip / Finish controls. Position picks the side
 * with the most room and clamps to viewport edges.
 *
 * Public API
 *   window.kmilaTutorial.start(steps, labels, options?)
 *     steps:   [{ target?: string, title: string, body: string }, ...]
 *     labels:  { next, back, skip, finish, stepOf } — pre-translated by Blazor
 *     options: { storageKey?: string }
 *
 *   window.kmilaTutorial.startWhenReady(steps, labels, options?)
 *     Same as start, but polls for the first step's target up to ~1.5 s. If
 *     the first step has no target (welcome / final card), runs immediately.
 *     Returns a Promise<boolean> — true if the tour actually launched.
 *
 *   window.kmilaTutorial.dismiss()
 *   window.kmilaTutorial.hasSeen(storageKey?)   — defaults to global flag
 *   window.kmilaTutorial.reset(storageKey?)     — no arg wipes every flag
 *
 * Persistence
 *   - Global welcome tour:        localStorage key `kmila.tutorialSeen`
 *   - Per-route tours (preferred): `kmila.tutorialSeen.<routeId>`
 *   - Global opt-out:             `kmila.tutorialsDisabled`
 *   Finish AND Skip (also Esc / backdrop-click) set only the CURRENT tour's key,
 *   so skipping one view still lets other views introduce themselves. The
 *   "Don't show tours again" link sets the opt-out, which suppresses all tours
 *   until reset(). When localStorage is unavailable the engine falls back to an
 *   in-memory record (show each tour once per session) rather than never showing.
 */
(function () {
    'use strict';

    var TUTORIAL_KEY_PREFIX = 'kmila.tutorialSeen';
    var TUTORIAL_SEEN_KEY   = TUTORIAL_KEY_PREFIX; // legacy / global welcome
    // Single global opt-out. Deliberately NOT under the `kmila.tutorialSeen`
    // prefix so a per-key reset() wildcard sweep doesn't clear it by accident;
    // reset() removes it explicitly. When set, hasSeen() reports every tour as
    // seen, so no tour ever fires until the user hits "Replay tours".
    var TUTORIAL_DISABLED_KEY = 'kmila.tutorialsDisabled';

    // In-memory fallbacks for when localStorage throws (private mode, locked-down
    // WebViews). They keep the "show each tour at most once per session" contract
    // even when nothing can be persisted — better than the old behaviour of
    // treating unavailable storage as "already seen" (tour never appeared).
    var sessionSeen = {};       // storageKey -> true
    var sessionDisabled = false;

    var root = null;          // root <div class="kmila-tour"> overlay
    var steps = [];
    var labels = {};
    var idx = 0;
    var lastDir = 1;          // +1 = advancing, -1 = going back; used to auto-skip missing steps
    var storageKey = TUTORIAL_SEEN_KEY;
    var keyHandler = null;
    var resizeHandler = null;
    var scrollHandler = null;

    function clampToViewport(rect, padding) {
        var vw = Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0);
        var vh = Math.max(document.documentElement.clientHeight || 0, window.innerHeight || 0);
        var x = Math.max(padding, Math.min(rect.x, vw - rect.w - padding));
        var y = Math.max(padding, Math.min(rect.y, vh - rect.h - padding));
        return { x: x, y: y, w: rect.w, h: rect.h };
    }

    function findTarget(selector) {
        if (!selector) return null;
        try {
            var el = document.querySelector(selector);
            if (!el) return null;
            // `offsetParent` returns null for position:fixed and display:none —
            // sidebar links are sticky-positioned so that check produces false
            // negatives. Use getBoundingClientRect's actual rendered size as
            // the visibility proxy instead.
            var r = el.getBoundingClientRect();
            return (r.width > 0 && r.height > 0) ? el : null;
        } catch (e) { return null; }
    }

    function buildSkeleton() {
        var overlay = document.createElement('div');
        overlay.className = 'kmila-tour';
        overlay.setAttribute('role', 'dialog');
        overlay.setAttribute('aria-modal', 'true');

        var backdrop = document.createElement('div');
        backdrop.className = 'kmila-tour__backdrop';
        overlay.appendChild(backdrop);

        var spotlight = document.createElement('div');
        spotlight.className = 'kmila-tour__spotlight';
        overlay.appendChild(spotlight);

        var card = document.createElement('div');
        card.className = 'kmila-tour__card';
        card.innerHTML = ''
            + '<div class="kmila-tour__step">' + (labels.stepOf || 'Step {0} of {1}') + '</div>'
            + '<h3 class="kmila-tour__title"></h3>'
            + '<p class="kmila-tour__body"></p>'
            + '<div class="kmila-tour__actions">'
            +   '<button type="button" class="kmila-tour__btn kmila-tour__btn--link" data-act="disable"></button>'
            +   '<button type="button" class="kmila-tour__btn kmila-tour__btn--ghost" data-act="skip"></button>'
            +   '<div class="kmila-tour__spacer"></div>'
            +   '<button type="button" class="kmila-tour__btn kmila-tour__btn--ghost" data-act="back"></button>'
            +   '<button type="button" class="kmila-tour__btn kmila-tour__btn--primary" data-act="next"></button>'
            + '</div>';
        overlay.appendChild(card);

        // Skip = "skip this view's tour only" (marks just the current key). The
        // separate "Don't show tours again" link is the deliberate global opt-out.
        card.querySelector('[data-act="disable"]').addEventListener('click', disableAll);
        card.querySelector('[data-act="skip"]').addEventListener('click', dismiss);
        card.querySelector('[data-act="back"]').addEventListener('click', goBack);
        card.querySelector('[data-act="next"]').addEventListener('click', goNext);
        backdrop.addEventListener('click', function (e) {
            // Click on backdrop = soft skip. Matches platform conventions
            // for modal overlays.
            if (e.target === backdrop) dismiss();
        });

        return overlay;
    }

    /*
     * Returns the first index at or after `from` (moving by `dir`) whose step is
     * renderable — a null target (centered card) or a target currently in the
     * DOM. Returns -1 if none. Lets the tour skip steps whose target is missing
     * (empty states, a page still hydrating) instead of showing a centered card
     * that describes an element the user can't see.
     */
    function firstRenderable(from, dir) {
        var i = from;
        var guard = 0;
        while (i >= 0 && i < steps.length && guard <= steps.length) {
            var s = steps[i];
            if (!s) return -1;
            if (!s.target || findTarget(s.target)) return i;
            i += dir;
            guard++;
        }
        return -1;
    }

    function render() {
        if (!root) return;

        // Auto-skip a step whose target has gone missing, in the direction of
        // travel. If nothing is renderable ahead, try the other direction; if the
        // whole tour is unrenderable, finish gracefully (marks it seen).
        var current = steps[idx];
        if (current && current.target && !findTarget(current.target)) {
            var ni = firstRenderable(idx, lastDir);
            if (ni === -1) ni = firstRenderable(idx, -lastDir);
            if (ni === -1) { finish(); return; }
            idx = ni;
        }

        var step = steps[idx];
        if (!step) { finish(); return; }

        var stepEl    = root.querySelector('.kmila-tour__step');
        var titleEl   = root.querySelector('.kmila-tour__title');
        var bodyEl    = root.querySelector('.kmila-tour__body');
        var disableBtn= root.querySelector('[data-act="disable"]');
        var skipBtn   = root.querySelector('[data-act="skip"]');
        var backBtn   = root.querySelector('[data-act="back"]');
        var nextBtn   = root.querySelector('[data-act="next"]');
        var spotlight = root.querySelector('.kmila-tour__spotlight');
        var card      = root.querySelector('.kmila-tour__card');

        var stepLabel = (labels.stepOf || 'Step {0} of {1}')
            .replace('{0}', String(idx + 1))
            .replace('{1}', String(steps.length));
        stepEl.textContent = stepLabel;
        titleEl.textContent = step.title || '';
        bodyEl.textContent  = step.body  || '';

        disableBtn.textContent = labels.disable || "Don't show tours again";
        skipBtn.textContent = labels.skip   || 'Skip';
        backBtn.textContent = labels.back   || 'Back';
        nextBtn.textContent = (idx === steps.length - 1)
            ? (labels.finish || 'Done')
            : (labels.next   || 'Next');

        backBtn.style.visibility = (idx === 0) ? 'hidden' : 'visible';
        // On the last step the primary button becomes "Got it" (finish), so the
        // per-view Skip and the global opt-out are redundant there — hide both.
        var isLast = (idx === steps.length - 1);
        skipBtn.style.visibility    = isLast ? 'hidden' : 'visible';
        disableBtn.style.visibility = isLast ? 'hidden' : 'visible';

        // Position spotlight + card
        var target = findTarget(step.target);
        if (target) {
            var r = target.getBoundingClientRect();
            var pad = 8;
            spotlight.style.display = '';
            spotlight.style.left   = (r.left   - pad) + 'px';
            spotlight.style.top    = (r.top    - pad) + 'px';
            spotlight.style.width  = (r.width  + pad * 2) + 'px';
            spotlight.style.height = (r.height + pad * 2) + 'px';
            // Scroll target into view if it's off-screen.
            if (r.bottom < 0 || r.top > window.innerHeight) {
                target.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        } else {
            spotlight.style.display = 'none';
        }

        // Card placement: prefer below-right of target, fall back to centre.
        var cardW = 340;
        var cardH = 220;
        var cardX, cardY;
        if (target) {
            var tr = target.getBoundingClientRect();
            var gap = 16;
            // Pick side with most room.
            var roomBelow = window.innerHeight - tr.bottom;
            var roomAbove = tr.top;
            var roomRight = window.innerWidth  - tr.right;
            var roomLeft  = tr.left;
            var maxRoom = Math.max(roomBelow, roomAbove, roomRight, roomLeft);
            if (maxRoom === roomBelow) {
                cardX = tr.left;
                cardY = tr.bottom + gap;
            } else if (maxRoom === roomAbove) {
                cardX = tr.left;
                cardY = tr.top - cardH - gap;
            } else if (maxRoom === roomRight) {
                cardX = tr.right + gap;
                cardY = tr.top;
            } else {
                cardX = tr.left - cardW - gap;
                cardY = tr.top;
            }
        } else {
            // Centre the card when no target (welcome / final steps).
            cardX = (window.innerWidth  - cardW) / 2;
            cardY = (window.innerHeight - cardH) / 2;
        }
        var clamped = clampToViewport({ x: cardX, y: cardY, w: cardW, h: cardH }, 12);
        card.style.left = clamped.x + 'px';
        card.style.top  = clamped.y + 'px';
        card.style.width = cardW + 'px';
    }

    function goNext() {
        if (idx >= steps.length - 1) { finish(); return; }
        lastDir = 1;
        idx++;
        render();
    }

    function goBack() {
        if (idx <= 0) return;
        lastDir = -1;
        idx--;
        render();
    }

    function markSeen() {
        try { localStorage.setItem(storageKey, '1'); }
        catch (e) { sessionSeen[storageKey] = true; }
    }

    function finish()  { markSeen(); teardown(); }

    /* Skip / Esc / backdrop-click dismiss ONLY the current view's tour (same as
       finishing it). Each view still introduces itself the first time it's
       visited — skipping one no longer suppresses the rest. To turn every tour
       off at once the user clicks the explicit "Don't show tours again" link,
       which calls disableAll(). */
    function dismiss() { markSeen(); teardown(); }

    function disableAll() {
        try { localStorage.setItem(TUTORIAL_DISABLED_KEY, '1'); }
        catch (e) { sessionDisabled = true; }
        teardown();
    }

    function teardown() {
        if (!root) return;
        try { root.remove(); } catch (e) { root.parentNode && root.parentNode.removeChild(root); }
        root = null;
        if (keyHandler) document.removeEventListener('keydown', keyHandler);
        if (resizeHandler) window.removeEventListener('resize', resizeHandler);
        if (scrollHandler) window.removeEventListener('scroll', scrollHandler, true);
        keyHandler = resizeHandler = scrollHandler = null;
        steps = [];
        idx = 0;
        lastDir = 1;
        storageKey = TUTORIAL_SEEN_KEY;
    }

    function start(stepsIn, labelsIn, options) {
        if (root) teardown();
        steps  = Array.isArray(stepsIn) ? stepsIn.slice() : [];
        labels = labelsIn || {};
        storageKey = (options && options.storageKey) || TUTORIAL_SEEN_KEY;
        idx = 0;
        lastDir = 1;
        if (steps.length === 0) return;

        root = buildSkeleton();
        document.body.appendChild(root);

        keyHandler = function (e) {
            if (e.key === 'Escape') dismiss();
            else if (e.key === 'ArrowRight' || e.key === 'Enter') goNext();
            else if (e.key === 'ArrowLeft') goBack();
        };
        document.addEventListener('keydown', keyHandler);

        // Re-position on resize / scroll so the spotlight tracks its target.
        var raf = 0;
        var schedule = function () {
            if (raf) return;
            raf = requestAnimationFrame(function () { raf = 0; render(); });
        };
        resizeHandler = schedule;
        scrollHandler = schedule;
        window.addEventListener('resize', resizeHandler);
        window.addEventListener('scroll',  scrollHandler, true);

        render();
    }

    /*
     * Polls for the first step's target up to ~1.5 s. Useful when a tour
     * fires immediately after a route change, before the page's components
     * have hydrated their DOM. If the first step has no target (e.g. a
     * centred welcome card), launches immediately.
     */
    function startWhenReady(stepsIn, labelsIn, options) {
        return new Promise(function (resolve) {
            if (!Array.isArray(stepsIn) || stepsIn.length === 0) { resolve(false); return; }
            var first = stepsIn[0];
            // No DOM dependency → fire right away.
            if (!first || !first.target) {
                start(stepsIn, labelsIn, options);
                resolve(true);
                return;
            }
            var tries = 0;
            var maxTries = 15;     // 15 * 100 ms = 1.5 s budget
            var iv = setInterval(function () {
                tries++;
                if (findTarget(first.target)) {
                    clearInterval(iv);
                    start(stepsIn, labelsIn, options);
                    resolve(true);
                } else if (tries >= maxTries) {
                    clearInterval(iv);
                    resolve(false);
                }
            }, 100);
        });
    }

    function hasSeen(key) {
        var k = key || TUTORIAL_SEEN_KEY;
        try {
            // Global opt-out gates every tour off.
            if (localStorage.getItem(TUTORIAL_DISABLED_KEY) === '1') return true;
            return localStorage.getItem(k) === '1';
        } catch (e) {
            // Storage unavailable (private mode, locked-down WebView): fall back to
            // the in-memory record so each tour still shows at most once this
            // session, instead of never (the previous behaviour).
            return sessionDisabled || sessionSeen[k] === true;
        }
    }

    /*
     * reset() with no args clears every flag in localStorage that starts
     * with the tutorial prefix AND the global opt-out — used by the "Replay
     * tours" button, so tours fire again (and are re-enabled if turned off).
     * reset('kmila.tutorialSeen.editor') clears just that key.
     */
    function reset(key) {
        try {
            if (key) {
                localStorage.removeItem(key);
                delete sessionSeen[key];
                return;
            }
            var doomed = [];
            for (var i = 0; i < localStorage.length; i++) {
                var k = localStorage.key(i);
                if (k && k.indexOf(TUTORIAL_KEY_PREFIX) === 0) doomed.push(k);
            }
            doomed.forEach(function (k) { localStorage.removeItem(k); });
            localStorage.removeItem(TUTORIAL_DISABLED_KEY);
        } catch (e) {}
        // Clear the in-memory fallbacks too so a storage-less session replays.
        if (key) { delete sessionSeen[key]; }
        else { sessionSeen = {}; sessionDisabled = false; }
    }

    window.kmilaTutorial = {
        start: start,
        startWhenReady: startWhenReady,
        dismiss: dismiss,
        hasSeen: hasSeen,
        reset: reset
    };
})();
