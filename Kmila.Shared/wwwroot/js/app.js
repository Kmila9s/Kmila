// Kmila Application JavaScript Utilities
//
// Pointer-event based interactions for unified mouse + touch + pen input.
// All gesture utilities here use PointerEvents so they work identically on
// desktop, touch screens, and stylus-driven hybrids.

/**
 * Downloads a file with the specified name and content.
 */
window.downloadFile = function (fileName, content) {
    const blob = new Blob([content], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
};

/* ─── Tab drag-and-drop, Pointer-Events edition ────────────────────────────
 * Replaces the HTML5 dragstart-based wiring. Works on touch, mouse, and pen.
 * Strategy:
 *   • pointerdown on a [data-kmila-tab]: record source pane/tab + start coords
 *   • after the user moves >TAB_DRAG_THRESHOLD px we declare a drag and clone
 *     the tab into a floating ghost following the pointer
 *   • pointermove updates ghost position and finds a hovered tab via
 *     elementFromPoint — that tab gets a visible drop-target ring
 *   • pointerup over a tab fires kmilaTabDragDrop which Blazor reads via
 *     kmilaConsumeTabDrop on the destination strip's @onpointerup
 *   • pointercancel / Esc aborts cleanly
 *
 * Backwards-compat: kmilaGetTabDrag / kmilaClearTabDrag / kmilaSetupTabDrag
 * remain callable so existing Razor code keeps working without edits.
 * ----------------------------------------------------------------------- */
(function () {
    const TAB_DRAG_THRESHOLD = 6; // px before we commit to a drag
    let session = null;           // { srcPane, srcTab, startX, startY, ghost, lastTarget }
    let pendingDrop = null;        // { srcPane, srcTab, destPane, destTab }

    function findTab(node) {
        return node && node.closest ? node.closest('[data-kmila-tab]') : null;
    }

    function clearDropTargets() {
        document.querySelectorAll('.is-drop-target').forEach(el =>
            el.classList.remove('is-drop-target'));
    }

    function buildGhost(sourceEl, x, y) {
        const rect = sourceEl.getBoundingClientRect();
        const ghost = sourceEl.cloneNode(true);
        ghost.classList.add('kmila-tab-ghost');
        ghost.style.position = 'fixed';
        ghost.style.left = (rect.left) + 'px';
        ghost.style.top  = (rect.top)  + 'px';
        ghost.style.width = rect.width + 'px';
        ghost.style.height = rect.height + 'px';
        ghost.style.pointerEvents = 'none';
        ghost.style.opacity = '0.85';
        ghost.style.zIndex = '9999';
        ghost.style.transform = 'translate3d(0,0,0)';
        ghost.style.transition = 'none';
        ghost.style.boxShadow = '0 6px 18px rgba(15,23,42,0.25)';
        ghost.style.borderRadius = getComputedStyle(sourceEl).borderRadius || '6px';
        ghost.dataset.ghostStartX = rect.left;
        ghost.dataset.ghostStartY = rect.top;
        document.body.appendChild(ghost);
        return ghost;
    }

    function endSession(commit) {
        if (!session) return;
        if (session.ghost && session.ghost.parentNode) {
            session.ghost.parentNode.removeChild(session.ghost);
        }
        if (session.sourceEl) {
            session.sourceEl.classList.remove('is-being-dragged');
        }
        clearDropTargets();
        const s = session;
        session = null;
        if (commit && s.lastTarget) {
            const destPane = parseInt(s.lastTarget.getAttribute('data-kmila-pane'), 10);
            const destTab  = parseInt(s.lastTarget.getAttribute('data-kmila-tab'),  10);
            if (!isNaN(destTab)) {
                pendingDrop = {
                    srcPane: s.srcPane,
                    srcTab:  s.srcTab,
                    destPane: isNaN(destPane) ? 0 : destPane,
                    destTab
                };
            }
        }
    }

    document.addEventListener('pointerdown', function (e) {
        // Only primary button / first touch
        if (e.button !== undefined && e.button !== 0 && e.pointerType === 'mouse') return;
        const tab = findTab(e.target);
        if (!tab) return;
        // Don't start a drag when the user clicked the close button
        if (e.target.closest && e.target.closest('.editor-tab__close')) return;
        const srcPane = parseInt(tab.getAttribute('data-kmila-pane'), 10);
        const srcTab  = parseInt(tab.getAttribute('data-kmila-tab'),  10);
        if (isNaN(srcTab)) return;
        session = {
            srcPane: isNaN(srcPane) ? 0 : srcPane,
            srcTab,
            startX: e.clientX,
            startY: e.clientY,
            ghost: null,
            sourceEl: tab,
            lastTarget: null,
            committed: false,
            pointerId: e.pointerId
        };
    }, true);

    document.addEventListener('pointermove', function (e) {
        if (!session) return;
        if (session.pointerId !== undefined && e.pointerId !== session.pointerId) return;
        const dx = e.clientX - session.startX;
        const dy = e.clientY - session.startY;
        if (!session.ghost && (dx*dx + dy*dy) > (TAB_DRAG_THRESHOLD*TAB_DRAG_THRESHOLD)) {
            session.ghost = buildGhost(session.sourceEl, session.startX, session.startY);
            session.sourceEl.classList.add('is-being-dragged');
            // Intentionally NOT calling setPointerCapture: with capture, pointerup
            // fires on the source element regardless of where the user releases,
            // and Blazor's @onpointerup on the destination tab never runs — the
            // drop quietly turns into a self-drop and the reorder no-ops. We
            // already track movement via the document-level pointermove listener,
            // so capture isn't needed to keep the session alive.
        }
        if (!session.ghost) return;
        e.preventDefault();
        session.ghost.style.transform = `translate3d(${dx}px, ${dy}px, 0)`;
        // Resolve hovered drop target
        session.ghost.style.display = 'none';
        const under = document.elementFromPoint(e.clientX, e.clientY);
        session.ghost.style.display = '';
        const overTab = findTab(under);
        clearDropTargets();
        if (overTab && overTab !== session.sourceEl) {
            overTab.classList.add('is-drop-target');
            session.lastTarget = overTab;
        } else {
            session.lastTarget = null;
        }
    }, { passive: false });

    document.addEventListener('pointerup', function (e) {
        if (!session) return;
        endSession(true);
    }, true);

    document.addEventListener('pointercancel', function (e) {
        endSession(false);
    }, true);

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && session) endSession(false);
    });

    /** Backwards-compat shims used by existing Blazor code. */
    window.kmilaSetupTabDrag = function () { /* no-op — handlers are global */ };

    /* ─── FilesCard drag-id store ──────────────────────────────────────────
     * The file tree uses HTML5 drag-and-drop. We can't rely on Blazor state
     * alone for the dragged ID because StateHasChanged re-renders during the
     * drag race with the DOM, and DragEventArgs doesn't expose dataTransfer.
     * Stash it here on dragstart and read it back on drop.
     * --------------------------------------------------------------------- */
    let kmilaFileDragId = null;
    window.kmilaSetFileDrag = function (id) { kmilaFileDragId = id; };
    window.kmilaGetFileDrag = function () { return kmilaFileDragId; };
    window.kmilaClearFileDrag = function () { kmilaFileDragId = null; };

    /* ─── Kebab popover positioning ────────────────────────────────────────
     * Blazor owns the open/close state for the FilesCard kebab. When the user
     * clicks a trigger, FilesCard asks for the trigger button's rect via
     * kmilaGetButtonRectByKey and only then renders the panel — so the panel
     * never paints at (0,0) covering other UI even if the JS round-trip fails.
     * --------------------------------------------------------------------- */
    window.kmilaGetButtonRectByKey = function (key) {
        const el = document.querySelector('[data-kmila-menu-key="' + CSS.escape(key) + '"]');
        if (!el) return null;
        const r = el.getBoundingClientRect();
        return {
            left: r.left, top: r.top, right: r.right, bottom: r.bottom,
            width: r.width, height: r.height
        };
    };
    window.kmilaViewportWidth = function () {
        return window.innerWidth || document.documentElement.clientWidth;
    };

    /* Export the rendered Flow-diagram SVG as either an SVG file or a PNG
       raster. Mermaid replaces our <pre.mermaid> with an inline <svg>; we
       grab that, inline the computed CSS so the saved file looks identical
       outside the host page, and stream the bytes to the user via the same
       anchor-download trick downloadFile uses.

       For PNG we draw the SVG onto an offscreen canvas at 2× device pixel
       ratio (visibly crisper on a Retina display) and toBlob the result.
       Failures get logged to the console — the caller swallows the JS
       exception so the editor stays usable. */
    window.kmilaDownloadFlowDiagram = async function (container, fileName, format) {
        try {
            // Accepts either an id (legacy) or a CSS selector. We also fall
            // back to the most recent rendered mermaid SVG anywhere on the
            // page so a misconfigured selector still produces a download.
            let root = null;
            if (typeof container === 'string') {
                if (container.startsWith('.') || container.startsWith('#') || container.includes(' ')) {
                    root = document.querySelector(container);
                } else {
                    root = document.getElementById(container) || document.querySelector(container);
                }
            }
            // Find the SVG: prefer one inside the container; fall back to the
            // mermaid output anywhere on the page if the container query fails.
            let svg = root && root.querySelector('svg');
            if (!svg) {
                console.warn('[kmilaDownloadFlowDiagram] container empty/missing for', container,
                    '— falling back to the first pre.mermaid svg on the page');
                svg = document.querySelector('.flow-diagram__body pre.mermaid svg')
                   || document.querySelector('pre.mermaid svg');
            }
            if (!svg) {
                console.error('[kmilaDownloadFlowDiagram] no rendered mermaid svg found anywhere');
                return false;
            }
            // Inline computed background so the saved file isn't transparent
            // over a different host. Mermaid leaves the <svg> background
            // unset by default.
            const clone = svg.cloneNode(true);
            const cs = getComputedStyle(svg);
            const bg = cs.backgroundColor && cs.backgroundColor !== 'rgba(0, 0, 0, 0)'
                ? cs.backgroundColor : '#ffffff';
            // Make sure width/height attributes exist on the SVG — some
            // mermaid builds rely on viewBox + CSS-only sizing which doesn't
            // round-trip into a standalone file.
            const rect = svg.getBoundingClientRect();
            const w = Math.max(1, Math.round(rect.width));
            const h = Math.max(1, Math.round(rect.height));
            clone.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
            clone.setAttribute('width', w);
            clone.setAttribute('height', h);
            clone.style.background = bg;

            const xml = new XMLSerializer().serializeToString(clone);
            const svgWithDecl = '<?xml version="1.0" encoding="UTF-8"?>\n' + xml;

            if (format === 'svg') {
                const blob = new Blob([svgWithDecl], { type: 'image/svg+xml;charset=utf-8' });
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url; a.download = fileName;
                document.body.appendChild(a); a.click(); document.body.removeChild(a);
                URL.revokeObjectURL(url);
                return true;
            }

            // PNG path — rasterize via a 2× canvas.
            const dpr = Math.max(1, window.devicePixelRatio || 1) * 2;
            const canvas = document.createElement('canvas');
            canvas.width  = w * dpr;
            canvas.height = h * dpr;
            const ctx = canvas.getContext('2d');
            ctx.scale(dpr, dpr);
            ctx.fillStyle = bg;
            ctx.fillRect(0, 0, w, h);

            const img = new Image();
            const dataUrl = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svgWithDecl);
            await new Promise((res, rej) => {
                img.onload = res; img.onerror = rej; img.src = dataUrl;
            });
            ctx.drawImage(img, 0, 0, w, h);
            await new Promise((res) => canvas.toBlob((blob) => {
                if (!blob) return res(false);
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url; a.download = fileName;
                document.body.appendChild(a); a.click(); document.body.removeChild(a);
                URL.revokeObjectURL(url);
                res(true);
            }, 'image/png'));
            return true;
        } catch (e) {
            console.error('[kmilaDownloadFlowDiagram]', e);
            return false;
        }
    };

    /* Compute body-local coordinates for the schematic info panel so it
       opens next to the clicked element instead of always at top-right.
       Inputs are viewport-relative (e.clientX / e.clientY); we translate
       into the body's scrollable space and clamp so panelW × panelH fits.
       When the click lands near the right/bottom edge we flip to the
       opposite side of the click rather than overflowing. */
    window.kmilaSchematicInfoPos = function (bodySelector, clientX, clientY, panelW, panelH) {
        const body = document.querySelector(bodySelector);
        if (!body) return null;
        const r = body.getBoundingClientRect();
        const offset = 14; // small gap so the panel doesn't sit on top of the click
        const margin = 8;
        const w = panelW || 320, h = panelH || 280;
        // Local (in-body, scroll-included) coords for the click point.
        const cx = clientX - r.left + body.scrollLeft;
        const cy = clientY - r.top  + body.scrollTop;
        // Default placement: down-right of the click.
        let x = cx + offset;
        let y = cy + offset;
        const visMaxX = body.scrollLeft + body.clientWidth  - margin;
        const visMaxY = body.scrollTop  + body.clientHeight - margin;
        if (x + w > visMaxX) x = cx - w - offset;            // flip to the left
        if (x < body.scrollLeft + margin) x = body.scrollLeft + margin;
        if (y + h > visMaxY) y = cy - h - offset;            // flip to the top
        if (y < body.scrollTop + margin) y = body.scrollTop + margin;
        return { x: x, y: y };
    };

    /* Open the FPGA picker programmatically. Path of opens after the TopBar
       refactor: ProfileMenu (outer <details>) → FPGA section (inner <details>).
       The picker is now nested inside the consolidated profile dropdown, so
       we have to expand both layers in order. Falls back to a bare picker
       if no profile menu is present (older layouts / lessons preview). */
    window.kmilaOpenFpgaPicker = function () {
        // Open the profile menu first when present so the inner picker is
        // actually in the rendered DOM tree (some browsers skip layout for
        // closed details children, which would break our inner queries).
        const profile = document.querySelector('details.kmila-profile-menu');
        if (profile) profile.open = true;

        // Inner FPGA details — prefer the inline variant inside the profile
        // menu, then fall back to any standalone instance for older layouts.
        const el = document.querySelector('details.kmila-profile-menu details.kmila-fpga-picker')
                 || document.querySelector('details.kmila-fpga-picker');
        if (!el) return false;
        el.open = true;
        try { el.scrollIntoView({ block: 'nearest', behavior: 'smooth' }); } catch (_) {}
        const search = el.querySelector('input.form-control');
        if (search) try { search.focus(); } catch (_) {}
        return true;
    };

    window.kmilaGetTabDrag = function () {
        if (pendingDrop) {
            return { paneIdx: pendingDrop.srcPane, tabIdx: pendingDrop.srcTab };
        }
        if (session) {
            return { paneIdx: session.srcPane, tabIdx: session.srcTab };
        }
        return { paneIdx: -1, tabIdx: -1 };
    };
    window.kmilaClearTabDrag = function () { pendingDrop = null; };
})();


/* ─── Pan + zoom utility for SVG visualizers ───────────────────────────────
 * Wires PointerEvents to a wrapper element so its child SVG can be panned
 * (1 pointer drag) and zoomed (2-pointer pinch on touch, ctrl+wheel on
 * desktop, or +/- buttons that call kmilaPanZoomZoom).
 *
 * The transform is applied to the SVG's wrapper via CSS `transform: translate
 * + scale`, NOT viewBox manipulation — that way the existing click handlers
 * on cells and wires keep working without recomputing hit testing.
 *
 * Public API:
 *   kmilaAttachPanZoom(rootSelector, opts) → returns a teardown id
 *   kmilaDetachPanZoom(id)
 *   kmilaPanZoomZoom(id, factor)
 *   kmilaPanZoomReset(id)
 * ----------------------------------------------------------------------- */
(function () {
    const handles = new Map();
    let nextId = 1;

    function applyTransform(state) {
        if (!state.target) return;
        state.target.style.transformOrigin = '0 0';
        state.target.style.transform =
            `translate3d(${state.tx}px, ${state.ty}px, 0) scale(${state.scale})`;
    }

    function clamp(n, lo, hi) { return Math.max(lo, Math.min(hi, n)); }

    function dist(a, b) {
        const dx = a.x - b.x, dy = a.y - b.y;
        return Math.sqrt(dx*dx + dy*dy);
    }

    function attach(rootSelector, opts) {
        opts = opts || {};
        const root = (typeof rootSelector === 'string')
            ? document.querySelector(rootSelector)
            : rootSelector;
        if (!root) return -1;
        const target = root.querySelector(opts.targetSelector || 'svg');
        if (!target) return -1;

        // Already attached? tear down first so re-renders don't double-bind.
        if (root.__kmilaPanZoomId) {
            detach(root.__kmilaPanZoomId);
        }

        const id = nextId++;
        const pointers = new Map();
        const state = {
            id, root, target, opts,
            tx: 0, ty: 0, scale: 1,
            minScale: opts.minScale || 0.2,
            maxScale: opts.maxScale || 6,
            // Pinch state
            pinchStartDist: 0,
            pinchStartScale: 1,
            pinchCenter: null,
            // Pan state
            panAnchorX: 0, panAnchorY: 0,
            panStartTx: 0, panStartTy: 0,
            panning: false
        };

        function onPointerDown(e) {
            // Don't start gestures from interactive child elements (buttons,
            // inputs in the toolbar). The root's own pointer handler still
            // wins because we only attach to the body, not the toolbar.
            if (e.target.closest('button, input, select, textarea, a')) return;
            pointers.set(e.pointerId, { x: e.clientX, y: e.clientY });
            // Intentionally NOT calling setPointerCapture here. Capturing on
            // pointerdown forces the subsequent click event to fire on root
            // (per the pointer-events compat-mouse spec when pointerdown and
            // pointerup target the same captured element), which means clicks
            // on schematic <path>/<g>/<port> elements get redirected to the
            // SVG's @onclick="ClearSelection" handler — the user can't pick
            // wires/cells anymore. We only capture once real movement crosses
            // the threshold (see onPointerMove). Below threshold this stays a
            // plain click and the schematic's children-level handlers fire.
            if (pointers.size === 1) {
                state.panAnchorX = e.clientX;
                state.panAnchorY = e.clientY;
                state.panStartTx = state.tx;
                state.panStartTy = state.ty;
                state.panning = true;
                state.pointerCaptured = false;
            } else if (pointers.size === 2) {
                state.panning = false;
                const pts = Array.from(pointers.values());
                state.pinchStartDist = dist(pts[0], pts[1]);
                state.pinchStartScale = state.scale;
                state.pinchCenter = {
                    x: (pts[0].x + pts[1].x) / 2,
                    y: (pts[0].y + pts[1].y) / 2
                };
                // For pinch we DO capture immediately — both fingers must be
                // tracked even if they leave the SVG bounds, and a 2-finger
                // gesture is unambiguously not a click.
                try { root.setPointerCapture(e.pointerId); state.pointerCaptured = true; } catch (_) {}
            }
        }

        function onPointerMove(e) {
            if (!pointers.has(e.pointerId)) return;
            pointers.set(e.pointerId, { x: e.clientX, y: e.clientY });
            if (pointers.size === 1 && state.panning) {
                const dx = e.clientX - state.panAnchorX;
                const dy = e.clientY - state.panAnchorY;
                // Threshold: avoid jitter eating clicks
                if (Math.abs(dx) < 2 && Math.abs(dy) < 2 && state.tx === state.panStartTx && state.ty === state.panStartTy) {
                    return;
                }
                // Real pan motion has started — capture the pointer NOW so the
                // drag survives if the user's finger leaves the SVG bounds. We
                // delayed this from pointerdown specifically so single-tap
                // clicks still hit child elements (wires, gates, ports).
                if (!state.pointerCaptured) {
                    try { root.setPointerCapture(e.pointerId); state.pointerCaptured = true; } catch (_) {}
                }
                state.tx = state.panStartTx + dx;
                state.ty = state.panStartTy + dy;
                applyTransform(state);
                e.preventDefault();
            } else if (pointers.size === 2 && state.pinchStartDist > 0) {
                const pts = Array.from(pointers.values());
                const d = dist(pts[0], pts[1]);
                if (d <= 0) return;
                const newScale = clamp(
                    state.pinchStartScale * (d / state.pinchStartDist),
                    state.minScale, state.maxScale);
                // Zoom around the pinch center: keep that screen point fixed.
                const rect = root.getBoundingClientRect();
                const cx = state.pinchCenter.x - rect.left;
                const cy = state.pinchCenter.y - rect.top;
                // World coords under the pinch center before the scale change
                const worldX = (cx - state.tx) / state.scale;
                const worldY = (cy - state.ty) / state.scale;
                state.scale = newScale;
                state.tx = cx - worldX * state.scale;
                state.ty = cy - worldY * state.scale;
                applyTransform(state);
                e.preventDefault();
            }
        }

        function onPointerUp(e) {
            pointers.delete(e.pointerId);
            try { root.releasePointerCapture(e.pointerId); } catch (_) {}
            if (pointers.size < 2) {
                state.pinchStartDist = 0;
            }
            if (pointers.size === 0) {
                state.panning = false;
            } else if (pointers.size === 1) {
                // Switching from pinch back to pan — re-anchor.
                const remaining = Array.from(pointers.values())[0];
                state.panAnchorX = remaining.x;
                state.panAnchorY = remaining.y;
                state.panStartTx = state.tx;
                state.panStartTy = state.ty;
                state.panning = true;
            }
        }

        function onWheel(e) {
            // Only zoom when ctrl/cmd held (matches Figma/Miro convention).
            // Otherwise let the browser scroll the body normally.
            if (!(e.ctrlKey || e.metaKey)) return;
            e.preventDefault();
            const factor = Math.exp(-e.deltaY * 0.0015);
            const newScale = clamp(state.scale * factor, state.minScale, state.maxScale);
            const rect = root.getBoundingClientRect();
            const cx = e.clientX - rect.left;
            const cy = e.clientY - rect.top;
            const worldX = (cx - state.tx) / state.scale;
            const worldY = (cy - state.ty) / state.scale;
            state.scale = newScale;
            state.tx = cx - worldX * state.scale;
            state.ty = cy - worldY * state.scale;
            applyTransform(state);
        }

        function onDblClick(e) {
            if (e.target.closest('button, input, select, textarea, a')) return;
            // Quick reset on desktop.
            state.tx = 0; state.ty = 0; state.scale = 1;
            applyTransform(state);
        }

        root.addEventListener('pointerdown', onPointerDown);
        root.addEventListener('pointermove', onPointerMove, { passive: false });
        root.addEventListener('pointerup', onPointerUp);
        root.addEventListener('pointercancel', onPointerUp);
        root.addEventListener('wheel', onWheel, { passive: false });
        root.addEventListener('dblclick', onDblClick);
        root.style.touchAction = 'none';
        root.__kmilaPanZoomId = id;

        handles.set(id, {
            state,
            teardown() {
                root.removeEventListener('pointerdown', onPointerDown);
                root.removeEventListener('pointermove', onPointerMove);
                root.removeEventListener('pointerup', onPointerUp);
                root.removeEventListener('pointercancel', onPointerUp);
                root.removeEventListener('wheel', onWheel);
                root.removeEventListener('dblclick', onDblClick);
                if (target) target.style.transform = '';
                root.style.touchAction = '';
                delete root.__kmilaPanZoomId;
            }
        });
        return id;
    }

    function detach(id) {
        const h = handles.get(id);
        if (!h) return;
        h.teardown();
        handles.delete(id);
    }

    function zoom(id, factor) {
        const h = handles.get(id);
        if (!h) return;
        const s = h.state;
        const rect = s.root.getBoundingClientRect();
        const cx = rect.width / 2;
        const cy = rect.height / 2;
        const worldX = (cx - s.tx) / s.scale;
        const worldY = (cy - s.ty) / s.scale;
        s.scale = Math.max(s.minScale, Math.min(s.maxScale, s.scale * factor));
        s.tx = cx - worldX * s.scale;
        s.ty = cy - worldY * s.scale;
        applyTransform(s);
    }

    function reset(id) {
        const h = handles.get(id);
        if (!h) return;
        h.state.tx = 0;
        h.state.ty = 0;
        h.state.scale = 1;
        applyTransform(h.state);
    }

    window.kmilaAttachPanZoom = attach;
    window.kmilaDetachPanZoom = detach;
    window.kmilaPanZoomZoom  = zoom;
    window.kmilaPanZoomReset = reset;
})();


/* ─── Tiny haptic helper (Android-only; iOS Safari ignores) ──────────────── */
window.kmilaHaptic = function (kind) {
    if (!navigator.vibrate) return;
    try {
        switch (kind) {
            case 'tick':    navigator.vibrate(8); break;
            case 'success': navigator.vibrate([12, 30, 12]); break;
            case 'error':   navigator.vibrate([20, 40, 20]); break;
            default:        navigator.vibrate(10);
        }
    } catch (_) {}
};


/* ─── Bottom-sheet dock controller (mobile) ──────────────────────────────────
 * Below 768px we treat the dock as a 3-snap-point bottom sheet. Snap points
 * are stored as data attributes on the dock element (peek/half/full) so CSS
 * keeps full control of the visual states and JS only updates a class.
 *
 * The grabber on the dock (handled separately by kmilaDockDragStart) keeps
 * working — when this controller is active it intercepts the drag and snaps
 * to the nearest point on release. */
window.kmilaSetDockSnap = function (point) {
    const dock = document.querySelector('.kmila-dock');
    if (!dock) return;
    dock.classList.remove('kmila-dock--snap-peek', 'kmila-dock--snap-half', 'kmila-dock--snap-full');
    if (point === 'peek' || point === 'half' || point === 'full') {
        dock.classList.add('kmila-dock--snap-' + point);
        try { localStorage.setItem('kmila-dock-snap', point); } catch (_) {}
    }
};

/* Called from EditorDock after a tab tap on mobile so the bottom-sheet
 * auto-opens to reveal the panel content (entities / schematic / flow /
 * ports / …). Without this the default peek state shows the tab strip but
 * keeps `.kmila-dock__panel` at `display: none`, so the user sees nothing
 * happen on tap.
 *
 *   wasActive=true   tap re-hit the already-active tab → close to peek
 *                    (gives the user a way to dismiss the sheet without
 *                    digging for the chevron)
 *   wasActive=false  tab changed (or dock was peek/collapsed) → snap to half
 *                    if not already half/full. Already at full?  leave alone
 *                    so we don't shrink a deliberately-maximised sheet. */
window.kmilaDockToggleForTab = function (wasActive) {
    if (!window.matchMedia('(max-width: 768px)').matches) return;
    // Defer to the next tick so the class flip runs *after* Blazor's pending
    // re-render (triggered by SetActive). If we ran synchronously and the
    // user was previously collapsed, Blazor would rewrite the dock's class
    // attribute and wipe the snap class we just added.
    setTimeout(function () {
        const dock = document.querySelector('.kmila-dock');
        if (!dock) return;
        const isHalf = dock.classList.contains('kmila-dock--snap-half');
        const isFull = dock.classList.contains('kmila-dock--snap-full');
        if (wasActive && (isHalf || isFull)) {
            window.kmilaSetDockSnap('peek');
            return;
        }
        if (!isHalf && !isFull) {
            window.kmilaSetDockSnap('half');
        }
    }, 0);
};


/* ─── Practice: Monaco visual region lock for FillGap exercises ──────────────
 * Phase 1 — visual layer only. The actual enforcement happens server-side in
 * PracticeEvaluator.SpliceFillGap (which extracts only the user's
 * BEGIN_EDIT/END_EDIT block and splices it into the canonical starter), so
 * any tampering students do here cannot fool the grader.
 *
 * Adds a faded background to lines outside the BEGIN_EDIT/END_EDIT block
 * and a subtle highlight on the editable region. Re-runs after every model
 * content change so the decorations stick if the user types and shifts
 * line numbers. */
const _kmilaPracticeDecos = new WeakMap();

window.kmilaPracticeApplyLock = function () {
    if (typeof monaco === 'undefined' || !monaco.editor) return;
    const editors = monaco.editor.getEditors();
    if (!editors || editors.length === 0) return;

    const editor = editors.find(e => {
        const dom = e.getDomNode && e.getDomNode();
        return dom && dom.closest && dom.closest('.practice-monaco');
    });
    if (!editor) return;

    const model = editor.getModel();
    if (!model) return;

    const lineCount = model.getLineCount();
    let beginLine = -1, endLine = -1;
    for (let ln = 1; ln <= lineCount; ln++) {
        const text = model.getLineContent(ln);
        if (beginLine < 0 && text.indexOf('BEGIN_EDIT') >= 0) beginLine = ln;
        else if (beginLine > 0 && endLine < 0 && text.indexOf('END_EDIT') >= 0) { endLine = ln; break; }
    }

    let newDecos = [];
    if (beginLine > 0 && endLine > beginLine) {
        if (beginLine > 1) {
            newDecos.push({
                range: new monaco.Range(1, 1, beginLine, 1),
                options: { isWholeLine: true, className: 'practice-monaco--locked' }
            });
        }
        if (endLine < lineCount) {
            newDecos.push({
                range: new monaco.Range(endLine + 1, 1, lineCount + 1, 1),
                options: { isWholeLine: true, className: 'practice-monaco--locked' }
            });
        }
        if (endLine - 1 >= beginLine + 1) {
            newDecos.push({
                range: new monaco.Range(beginLine + 1, 1, endLine, 1),
                options: { isWholeLine: true, className: 'practice-monaco--editable' }
            });
        }
    }

    const prev = _kmilaPracticeDecos.get(editor) || [];
    const next = editor.deltaDecorations(prev, newDecos);
    _kmilaPracticeDecos.set(editor, next);

    // Re-apply on every model content change so the regions track typing.
    // Bind the handler once per editor instance.
    if (!editor._kmilaPracticeBound) {
        editor._kmilaPracticeBound = true;
        editor.onDidChangeModelContent(() => {
            // Defer to the next tick so this runs *after* Monaco has applied
            // the user's edit; otherwise the decoration ids reference a
            // stale model version.
            setTimeout(() => window.kmilaPracticeApplyLock(), 0);
        });
    }
};

window.kmilaPracticeClearLock = function () {
    if (typeof monaco === 'undefined' || !monaco.editor) return;
    const editors = monaco.editor.getEditors();
    for (const editor of editors) {
        const dom = editor.getDomNode && editor.getDomNode();
        if (!dom || !dom.closest || !dom.closest('.practice-monaco')) continue;
        const prev = _kmilaPracticeDecos.get(editor) || [];
        if (prev.length > 0) {
            editor.deltaDecorations(prev, []);
            _kmilaPracticeDecos.set(editor, []);
        }
    }
};
