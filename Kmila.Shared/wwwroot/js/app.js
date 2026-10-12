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

    /* Pin the drag ghost to the row being dragged. Two layered problems:
     *   1. Default browser ghost for `.file-row` (grid item, position:relative,
     *      gap:2px) snapshots more than the row — looks like the whole card.
     *   2. Passing the live row to setDragImage isn't enough either: Blink/WebKit
     *      rasterizes the element's *paint layer*, and `.file-row` doesn't
     *      establish its own (no transform/opacity/isolation), so the snapshot
     *      still picks up sibling rows from the shared <ul> layer.
     * Fix: clone the row, mount it offscreen on <body> (its own paint context),
     * use the clone as the drag image, then remove it on the next tick — the
     * browser has already taken the bitmap by then. Capture phase so we win the
     * race against Blazor re-renders. */
    document.addEventListener('dragstart', function (e) {
        const t = e.target;
        const row = (t && t.closest) ? t.closest('.file-row[draggable="true"]') : null;
        if (!row || !e.dataTransfer) return;
        try {
            const r = row.getBoundingClientRect();
            const ox = Math.min(Math.max(0, e.clientX - r.left), r.width);
            const oy = Math.min(Math.max(0, e.clientY - r.top), r.height);
            const clone = row.cloneNode(true);
            clone.style.position = 'fixed';
            clone.style.top = '-10000px';
            clone.style.left = '0';
            clone.style.width = r.width + 'px';
            clone.style.margin = '0';
            clone.style.pointerEvents = 'none';
            document.body.appendChild(clone);
            e.dataTransfer.setDragImage(clone, ox, oy);
            setTimeout(function () { clone.remove(); }, 0);
        } catch (_) { /* fall back to browser default */ }
    }, true);

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

    /**
     * Sibling of kmilaViewportWidth — returns the current viewport height so
     * C# placement code can flip popovers upward when there isn't room below.
     * Used by FilesCard's file-row burger menu to keep the dropdown in view
     * for low rows (see [[project_file_menu_overflow]]).
     */
    window.kmilaViewportHeight = function () {
        return window.innerHeight || document.documentElement.clientHeight;
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
    // Locates the mermaid-rendered SVG inside `container` (a CSS selector or
    // id), inlines its computed background and width/height attributes, and
    // returns a {svg, w, h, bg, ok} record. Shared by serialize/download
    // entry points so the SVG-discovery + cloning logic lives in one place.
    // Walk source + clone trees in lockstep and copy a small set of visual
    // computed-style properties from live → clone as inline `style="..."`,
    // then aggressively strip every node / attribute that could trigger an
    // external fetch when the rasteriser loads the standalone SVG. Without
    // this, mermaid's <style> @imports Google Fonts and toBlob throws
    // "Tainted canvases may not be exported". Logs sanitisation stats so a
    // future report of "still tainted" can be diagnosed against the actual
    // tree the function saw. Backlog #2.
    //
    // NOTE: deliberately does NOT inline font-family from getComputedStyle
    // — mermaid resolves to "Trebuchet MS, Lucida Sans Unicode, sans-serif"
    // or similar; baking that into the standalone SVG can still trip a font
    // probe on engines that look up the family before falling back. We pin
    // every element to a guaranteed-local system stack instead.
    function _kmilaInlineSvgStyles(srcRoot, dstRoot) {
        const PROPS = [
            'fill', 'fill-opacity', 'stroke', 'stroke-width', 'stroke-dasharray',
            'stroke-linecap', 'stroke-linejoin', 'opacity',
            'font-size', 'font-weight', 'font-style',
            'text-anchor', 'dominant-baseline', 'color'
        ];
        let nodesVisited = 0;
        const stack = [[srcRoot, dstRoot]];
        while (stack.length) {
            const [s, d] = stack.pop();
            nodesVisited++;
            try {
                const cs = getComputedStyle(s);
                let inline = 'font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;';
                for (let i = 0; i < PROPS.length; i++) {
                    const v = cs.getPropertyValue(PROPS[i]);
                    if (v) inline += PROPS[i] + ':' + v + ';';
                }
                const existing = d.getAttribute('style') || '';
                d.setAttribute('style', inline + existing);
            } catch (_) { /* SVG nodes without computed styles — skip */ }
            const sc = s.children, dc = d.children;
            const n = Math.min(sc.length, dc.length);
            for (let i = 0; i < n; i++) stack.push([sc[i], dc[i]]);
        }

        const stats = { nodesVisited, stylesStripped: 0, foreignStripped: 0,
                        externalImagesStripped: 0, externalUseStripped: 0,
                        externalHrefsCleared: 0 };

        // Each of these is a documented canvas-taint source. We remove them
        // outright rather than trying to rewrite — the export needs to
        // succeed even if it means dropping some decorative element.
        dstRoot.querySelectorAll('style').forEach(function (el) {
            el.remove(); stats.stylesStripped++;
        });
        dstRoot.querySelectorAll('foreignObject').forEach(function (el) {
            el.remove(); stats.foreignStripped++;
        });
        const isExternal = function (href) {
            return !!href && !href.startsWith('#') && !href.startsWith('data:');
        };
        dstRoot.querySelectorAll('image').forEach(function (el) {
            const h = el.getAttribute('href') || el.getAttributeNS('http://www.w3.org/1999/xlink', 'href');
            if (isExternal(h)) { el.remove(); stats.externalImagesStripped++; }
        });
        dstRoot.querySelectorAll('use').forEach(function (el) {
            const h = el.getAttribute('href') || el.getAttributeNS('http://www.w3.org/1999/xlink', 'href');
            if (isExternal(h)) { el.remove(); stats.externalUseStripped++; }
        });
        // Belt-and-braces: any remaining external href / xlink:href on any
        // node (a, link, etc. mermaid-rare-but-possible) gets blanked.
        dstRoot.querySelectorAll('*').forEach(function (el) {
            ['href', 'xlink:href'].forEach(function (attr) {
                const v = el.getAttribute(attr);
                if (isExternal(v)) { el.removeAttribute(attr); stats.externalHrefsCleared++; }
            });
        });

        try { console.log('[kmilaInlineSvgStyles] sanitised', stats); } catch (_) {}
    }

    function _kmilaPrepFlowSvg(container) {
        let root = null;
        if (typeof container === 'string') {
            if (container.startsWith('.') || container.startsWith('#') || container.includes(' ')) {
                root = document.querySelector(container);
            } else {
                root = document.getElementById(container) || document.querySelector(container);
            }
        }
        let svg = root && root.querySelector('svg');
        if (!svg) {
            svg = document.querySelector('.flow-diagram__body pre.mermaid svg')
               || document.querySelector('pre.mermaid svg');
        }
        if (!svg) return { ok: false };

        const clone = svg.cloneNode(true);
        // Inline computed styles + strip <style> so the standalone SVG has
        // zero external references. Without this, mermaid's font @import
        // taints the rasterisation canvas.
        try { _kmilaInlineSvgStyles(svg, clone); }
        catch (e) { console.warn('[_kmilaPrepFlowSvg] style inline failed', e); }
        const cs = getComputedStyle(svg);
        const bg = cs.backgroundColor && cs.backgroundColor !== 'rgba(0, 0, 0, 0)'
            ? cs.backgroundColor : '#ffffff';
        // Strip the pan-zoom CSS transform the in-app viewer applies to the
        // live SVG root. Without this, the cloned SVG inherits the inline
        // translate/scale and the rasterised image draws content off-canvas
        // (reported: PNG export comes out completely blank). The export must
        // capture the *unzoomed* diagram, not the user's current pan/zoom.
        clone.style.transform = '';
        clone.style.transformOrigin = '';
        clone.removeAttribute('transform');
        // Resolve natural (pre-transform) dimensions. Prefer the viewBox so we
        // get the diagram's authoring-coordinate size; fall back to the SVG's
        // own width/height attributes; only use the live bounding rect as a
        // last resort (since that rect reflects the active pan-zoom).
        let w = 0, h = 0;
        const vb = svg.viewBox && svg.viewBox.baseVal;
        if (vb && vb.width > 0 && vb.height > 0) {
            w = vb.width; h = vb.height;
        } else if (svg.width && svg.width.baseVal && svg.width.baseVal.value > 0) {
            w = svg.width.baseVal.value;
            h = svg.height.baseVal.value;
        } else {
            const rect = svg.getBoundingClientRect();
            w = rect.width; h = rect.height;
        }
        w = Math.max(1, Math.round(w));
        h = Math.max(1, Math.round(h));
        clone.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
        // xlink namespace is required for the <use xlink:href="…"/> markers
        // mermaid emits (arrowheads / nodes). Without an explicit declaration
        // on the standalone SVG, the canvas rasteriser refuses to load the
        // image (silent — onerror fires, the export comes back as empty
        // bytes and the file never downloads). Backlog #2 root cause.
        clone.setAttribute('xmlns:xlink', 'http://www.w3.org/1999/xlink');
        clone.setAttribute('width', w);
        clone.setAttribute('height', h);
        clone.style.background = bg;

        const xml = new XMLSerializer().serializeToString(clone);
        const svgWithDecl = '<?xml version="1.0" encoding="UTF-8"?>\n' + xml;
        return { ok: true, svg: svgWithDecl, w, h, bg };
    }

    // Returns the serialized SVG text. Used by FlowDiagramViewer's export
    // path so the bytes flow through IFileExportService → MauiFileExportService
    // on MAUI (writes to ~/Documents/Kmila/SVG/) instead of the browser-only
    // anchor download (which iOS/macCatalyst WebViews drop on the floor).
    // Ensures the flow diagram's mermaid <svg> exists before serialising.
    // The on-screen diagram is produced by an ASYNC mermaid pipeline
    // (kmilaMermaidRun); export can be clicked before it finishes, or during a
    // theme-toggle teardown that briefly reverts pre.mermaid to raw text. The
    // old code failed on the first miss and surfaced a misleading "not ready"
    // toast. Instead we kick a render and poll briefly. Returns the prep object
    // on success; on failure a { ok:false, reason } where reason is:
    //   'no-diagram'    — no pre.mermaid in the DOM (nothing to export)
    //   'render-failed' — source present but mermaid never produced an <svg>
    async function _kmilaEnsureFlowSvg(container) {
        let prep = _kmilaPrepFlowSvg(container);
        if (prep.ok) return prep;

        // The viewer re-keys (@key) the <pre> on every render, so there's a
        // brief window where neither the source nor the rendered <svg> is in
        // the DOM. Don't declare "no diagram" on the first miss — give the DOM
        // one frame to settle and re-check, otherwise an export clicked right
        // as the diagram refreshes falsely reports "not ready".
        let hasSource = !!document.querySelector('.flow-diagram__body pre.mermaid, pre.mermaid');
        if (!hasSource) {
            await new Promise(function (r) { requestAnimationFrame(function () { r(); }); });
            hasSource = !!document.querySelector('.flow-diagram__body pre.mermaid, pre.mermaid');
            if (!hasSource) return { ok: false, reason: 'no-diagram' };
        }

        // Kick a render, then POLL for the <svg> — deliberately without awaiting
        // kmilaMermaidRun(): a large flowchart can take several seconds to lay
        // out, and kmilaMermaidRun's reentrancy guard resolves immediately when
        // another render is already in flight, so awaiting it would either hang
        // the export or return before the <svg> exists. Firing + polling with a
        // bounded ceiling recovers the diagram when it finishes and fails fast
        // (render-failed) otherwise, so the export never hangs.
        try { window.kmilaMermaidRun(); } catch (e) { /* fall through to poll */ }
        for (let i = 0; i < 200; i++) {          // ~10s ceiling (200 × 50ms)
            await new Promise(function (r) { setTimeout(r, 50); });
            prep = _kmilaPrepFlowSvg(container);
            if (prep.ok) return prep;
        }
        return { ok: false, reason: 'render-failed' };
    }

    // On failure returns an 'ERR:<reason>' sentinel (never a valid SVG, which
    // always starts with '<') so the C# caller can pick the right toast.
    window.kmilaSerializeFlowSvg = async function (container) {
        try {
            const r = await _kmilaEnsureFlowSvg(container);
            return r.ok ? r.svg : ('ERR:' + r.reason);
        } catch (e) {
            console.error('[kmilaSerializeFlowSvg]', e);
            return 'ERR:render-failed';
        }
    };

    // Rasterises the mermaid SVG to a PNG and returns base64 bytes. Counterpart
    // to kmilaSerializeFlowSvg for the PNG export path. Resolves '' on failure.
    //
    // Uses a Blob URL (not a data: URL) for the intermediate <img>. Mermaid
    // SVGs routinely contain inline <style> blocks, foreignObject HTML, and
    // unicode glyphs; piping that through encodeURIComponent for a data:
    // URL is fragile (long-SVG ceilings, fractional-percent encoding bugs in
    // some WebViews) and the load silently fails, which is what backlog #2
    // was hitting. The Blob path is what SchematicViewer's working PNG
    // export uses — same primitive, same robustness.
    window.kmilaSerializeFlowPng = function (container) {
        return new Promise(function (resolve) {
            _kmilaEnsureFlowSvg(container).then(function (prep) {
              try {
                if (!prep.ok) { resolve('ERR:' + prep.reason); return; }

                // A browser <canvas> has hard limits (~16384 px per side on
                // Chrome, lower on Safari/iOS; plus a max total AREA), and —
                // crucially — toBlob() can silently return null on a canvas
                // that is merely large (not over the documented cap) depending
                // on the platform / available memory. So instead of picking one
                // size and failing when it doesn't take, we build a ladder of
                // pixel ratios (crisp first) and rasterise at the first one that
                // actually yields bytes. This way the PNG ALWAYS exports — just
                // a bit softer for giant diagrams — rather than surfacing the
                // misleading "diagram not ready" toast. SVG export stays lossless.
                const MAX_DIM = 16384;
                const MAX_AREA = 16384 * 8192;
                let dpr = Math.max(1, window.devicePixelRatio || 1) * 2;
                dpr = Math.min(dpr, MAX_DIM / prep.w, MAX_DIM / prep.h,
                               Math.sqrt(MAX_AREA / (prep.w * prep.h)));
                const ratios = [];
                for (let r = dpr; r >= 0.12; r *= 0.6) ratios.push(r);
                if (ratios.length === 0) ratios.push(Math.max(0.1, dpr));

                const finish = function (blobUrl, val) {
                    if (blobUrl) { try { URL.revokeObjectURL(blobUrl); } catch (_) {} }
                    resolve(val);
                };

                // Rasterise the already-loaded <img> at ratios[i]; on any miss
                // (toBlob null, draw/encode throw, reader error) drop to the
                // next smaller ratio. Only when the whole ladder is exhausted do
                // we report a genuine PNG failure (→ "export as SVG").
                const rasterize = function (img, blobUrl) {
                    const tryAt = function (i) {
                        if (i >= ratios.length) { finish(blobUrl, 'ERR:png-failed'); return; }
                        const r = ratios[i];
                        try {
                            const canvas = document.createElement('canvas');
                            canvas.width  = Math.max(1, Math.floor(prep.w * r));
                            canvas.height = Math.max(1, Math.floor(prep.h * r));
                            const ctx = canvas.getContext('2d');
                            ctx.scale(r, r);
                            ctx.fillStyle = prep.bg;
                            ctx.fillRect(0, 0, prep.w, prep.h);
                            ctx.drawImage(img, 0, 0, prep.w, prep.h);
                            canvas.toBlob(function (blob) {
                                if (!blob) { tryAt(i + 1); return; }   // too big here → smaller
                                const reader = new FileReader();
                                reader.onloadend = function () {
                                    const result = String(reader.result || '');
                                    const comma = result.indexOf(',');
                                    if (comma >= 0) finish(blobUrl, result.substring(comma + 1));
                                    else tryAt(i + 1);
                                };
                                reader.onerror = function () { tryAt(i + 1); };
                                reader.readAsDataURL(blob);
                            }, 'image/png');
                        } catch (e) {
                            console.warn('[kmilaSerializeFlowPng] rasterise miss @' + r, e);
                            tryAt(i + 1);
                        }
                    };
                    tryAt(0);
                };

                // Load the SVG as an <img>. Prefer a Blob URL; if that fails to
                // load (some WebViews choke on blob: SVGs), retry once via a
                // data: URL before giving up.
                const svgBlob = new Blob([prep.svg], { type: 'image/svg+xml;charset=utf-8' });
                const blobUrl = URL.createObjectURL(svgBlob);
                const img = new Image();
                img.onload = function () { rasterize(img, blobUrl); };
                img.onerror = function () {
                    try { URL.revokeObjectURL(blobUrl); } catch (_) {}
                    const img2 = new Image();
                    img2.onload = function () { rasterize(img2, null); };
                    img2.onerror = function (e) {
                        console.error('[kmilaSerializeFlowPng] image load failed', e);
                        resolve('ERR:png-failed');
                    };
                    img2.src = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(prep.svg);
                };
                img.src = blobUrl;
              } catch (e) {
                console.error('[kmilaSerializeFlowPng]', e);
                resolve('ERR:png-failed');
              }
            }, function (e) {
                console.error('[kmilaSerializeFlowPng] ensure failed', e);
                resolve('ERR:render-failed');
            });
        });
    };

    // Ensures the flow diagram's mermaid <svg> is rendered, then attaches the
    // shared pan-zoom to it — returning the handle id (or -1). FlowDiagramViewer
    // calls this instead of kmilaAttachPanZoom directly: the on-screen diagram
    // is produced by an ASYNC mermaid pipeline (kmilaMermaidRun, which also
    // lazy-loads its flowchart grammar chunk on first use), so at OnAfterRender
    // time the <pre> is usually still raw text. A bare attach would see no <svg>,
    // return -1, and the zoom toolbar (which needs a live handle) would stay
    // dead even after the diagram appears. Here we kick the render and poll for
    // the <svg> (same primitive the export path uses) before binding, so the
    // handle reliably attaches no matter how long the first render takes.
    window.kmilaAttachFlowPanZoom = async function (rootSelector, opts) {
        try {
            const root = (typeof rootSelector === 'string')
                ? document.querySelector(rootSelector) : rootSelector;
            if (!root) return -1;
            const sel = (opts && opts.targetSelector) || 'svg';
            let svg = root.querySelector(sel);
            if (!svg) {
                const hasSource = !!(root.querySelector('pre.mermaid') || document.querySelector('pre.mermaid'));
                if (hasSource) {
                    try { window.kmilaMermaidRun(); } catch (e) { /* poll anyway */ }
                    for (let i = 0; i < 200 && !svg; i++) {       // ~10s ceiling
                        await new Promise(function (r) { setTimeout(r, 50); });
                        svg = root.querySelector(sel);
                    }
                }
            }
            if (!svg) return -1;
            return window.kmilaAttachPanZoom(root, opts);
        } catch (e) {
            console.error('[kmilaAttachFlowPanZoom]', e);
            return -1;
        }
    };

    // Legacy anchor-download path. Kept for the Web target (browser sandbox)
    // where IFileExportService delegates back to a download. FlowDiagramViewer
    // now prefers the serialize-then-IFileExportService route on all targets.
    window.kmilaDownloadFlowDiagram = async function (container, fileName, format) {
        try {
            const prep = _kmilaPrepFlowSvg(container);
            if (!prep.ok) {
                console.error('[kmilaDownloadFlowDiagram] no rendered mermaid svg found');
                return false;
            }
            if (format === 'svg') {
                const blob = new Blob([prep.svg], { type: 'image/svg+xml;charset=utf-8' });
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url; a.download = fileName;
                document.body.appendChild(a); a.click(); document.body.removeChild(a);
                URL.revokeObjectURL(url);
                return true;
            }
            const dpr = Math.max(1, window.devicePixelRatio || 1) * 2;
            const canvas = document.createElement('canvas');
            canvas.width  = prep.w * dpr;
            canvas.height = prep.h * dpr;
            const ctx = canvas.getContext('2d');
            ctx.scale(dpr, dpr);
            ctx.fillStyle = prep.bg;
            ctx.fillRect(0, 0, prep.w, prep.h);
            const img = new Image();
            const dataUrl = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(prep.svg);
            await new Promise((res, rej) => { img.onload = res; img.onerror = rej; img.src = dataUrl; });
            ctx.drawImage(img, 0, 0, prep.w, prep.h);
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

    function clamp(n, lo, hi) { return Math.max(lo, Math.min(hi, n)); }

    // Re-reads the target's natural (pre-transform) box. clientWidth/Height
    // ignore CSS transforms, so they're the right primitive here. SVG elements
    // expose those in modern browsers; fall back to bounding-rect-divided-by-
    // scale when they're 0 (older WebKit on iOS sometimes reports 0 for SVG).
    function measureNatural(state) {
        // Trust the caller's declared canvas size when given — it's exact and
        // immune to the clientWidth===0 / transform-inflation quirks below.
        if (state.naturalWHint > 0 && state.naturalHHint > 0) {
            state.naturalW = state.naturalWHint;
            state.naturalH = state.naturalHHint;
            return;
        }
        let w = state.target.clientWidth || 0;
        let h = state.target.clientHeight || 0;
        if (!w || !h) {
            const prev = state.target.style.transform;
            state.target.style.transform = '';
            const r = state.target.getBoundingClientRect();
            state.target.style.transform = prev;
            w = r.width / (state.scale || 1);
            h = r.height / (state.scale || 1);
        }
        if (w > 0) state.naturalW = w;
        if (h > 0) state.naturalH = h;
    }

    // Keeps at least MIN_VIS pixels of content inside the root on each axis.
    // Without this, a fast swipe / pinch on mobile can translate the whole
    // content offscreen — the schematic or waveform appears to "vanish" and
    // the user has no obvious way to recover (reported on Android + iOS).
    function clampPan(state) {
        if (state.naturalW <= 0 || state.naturalH <= 0) return;
        const rect = state.root.getBoundingClientRect();
        if (rect.width <= 0 || rect.height <= 0) return;
        const cw = state.naturalW * state.scale;
        const ch = state.naturalH * state.scale;
        const MIN_VIS = Number.isFinite(state.minVisible) ? state.minVisible : 80;
        // Account for any layout offset of target relative to root (padding
        // on .schematic-viewer__body etc.) so the clamp uses the same origin
        // the transform is composed against.
        const ox = state.target.offsetLeft || 0;
        const oy = state.target.offsetTop || 0;
        state.tx = clamp(state.tx, MIN_VIS - cw - ox, rect.width - MIN_VIS - ox);
        state.ty = clamp(state.ty, MIN_VIS - ch - oy, rect.height - MIN_VIS - oy);
    }

    function applyTransform(state) {
        if (!state.target) return;
        clampPan(state);
        state.target.style.transformOrigin = '0 0';
        state.target.style.transform =
            `translate3d(${state.tx}px, ${state.ty}px, 0) scale(${state.scale})`;
    }

    // Fit-to-view: scale content so it fills the root (with a small padding),
    // then translate so it sits centered. Used by the schematic on first
    // attach because its natural canvas (≥1000 px wide) only shows the
    // top-left corner inside a mobile body — felt "too far from the
    // schematic" per user feedback.
    function fitToView(state) {
        measureNatural(state);
        if (state.naturalW <= 0 || state.naturalH <= 0) return false;
        const rect = state.root.getBoundingClientRect();
        if (rect.width <= 20 || rect.height <= 20) return false;
        const pad = 16;
        const sx = (rect.width  - pad * 2) / state.naturalW;
        const sy = (rect.height - pad * 2) / state.naturalH;
        const fitScale = Math.min(sx, sy);
        // A wide schematic (CanvasWidth can be several thousand px) must be
        // allowed to shrink below the configured pan floor, or it stays
        // clipped on every edge. Lower the pan floor to the fitted scale so
        // the content can fit AND the user can't then zoom back out past the
        // fitted frame into empty space. Never exceed maxScale.
        if (fitScale > 0 && fitScale < state.minScale) state.minScale = fitScale;
        state.scale = clamp(fitScale, state.minScale, state.maxScale);
        const ox = state.target.offsetLeft || 0;
        const oy = state.target.offsetTop  || 0;
        state.tx = (rect.width  - state.naturalW * state.scale) / 2 - ox;
        state.ty = (rect.height - state.naturalH * state.scale) / 2 - oy;
        // A fresh fit reframes the content, so the view is no longer a
        // user-chosen one — let resize re-fit again until they adjust it.
        state.userAdjusted = false;
        applyTransform(state);
        return true;
    }

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
            // Minimum content-pixels of the target that must stay inside the
            // root on each axis when panning. Default 80 keeps schematics /
            // waveforms discoverable on desktop; callers (e.g. the Visual
            // canvas on mobile) pass a smaller value so widgets near the
            // outer edge stay reachable without an off-screen "lock".
            minVisible: Number.isFinite(opts.minVisible) ? Math.max(0, opts.minVisible) : 80,
            // Pinch state
            pinchStartDist: 0,
            pinchStartScale: 1,
            pinchCenter: null,
            // Pan state
            panAnchorX: 0, panAnchorY: 0,
            panStartTx: 0, panStartTy: 0,
            panning: false,
            // Natural (pre-transform) content size — measured at attach and
            // re-measured on resize. Used to clamp pan and to fit-to-view.
            naturalW: 0, naturalH: 0,
            // Caller-supplied natural size (the layout's own canvas pixels).
            // When present it is the source of truth for measureNatural,
            // sidestepping the browser quirks where an <svg> reports
            // clientWidth/Height === 0 (older WebKit) or a transformed
            // getBoundingClientRect has to be divided back by scale (which
            // inflates the box whenever scale !== 1). Pass 0/undefined to
            // keep the pure-DOM measurement path (waveform, Visual canvas).
            naturalWHint: Number.isFinite(opts.naturalWidth)  && opts.naturalWidth  > 0 ? opts.naturalWidth  : 0,
            naturalHHint: Number.isFinite(opts.naturalHeight) && opts.naturalHeight > 0 ? opts.naturalHeight : 0,
            // True once the user has manually panned/pinched/zoomed. While
            // false, a root resize (dock resize, fullscreen toggle, rotate)
            // re-fits so the content stays framed; once the user takes
            // control we stop auto-framing and only re-clamp, so their view
            // isn't yanked back on the next resize.
            userAdjusted: false,
            // Initial framing strategy. fitOnAttach=true centers and scales
            // the content into the root (schematic); false keeps the
            // identity transform so the user starts at the top-left of the
            // natural layout (waveform's horizontally-scrolling timeline).
            fitOnAttach: !!opts.fitOnAttach,
            needsFit: !!opts.fitOnAttach
        };

        // Extra selectors the caller wants us to leave alone — e.g. Visual
        // passes `['.kviz-widget']` so a pointerdown on a widget flows
        // through to the widget's own drag handler instead of starting a
        // canvas pan. Composed with the built-in interactive-element skip.
        const skipTargets = Array.isArray(opts.skipTargetSelectors)
            ? opts.skipTargetSelectors.filter(s => typeof s === 'string' && s.length)
            : [];
        function shouldSkip(el) {
            if (!el || !el.closest) return false;
            if (el.closest('button, input, select, textarea, a')) return true;
            for (let i = 0; i < skipTargets.length; i++) {
                if (el.closest(skipTargets[i])) return true;
            }
            return false;
        }

        function onPointerDown(e) {
            // Don't start gestures from interactive child elements (buttons,
            // inputs in the toolbar) OR from any caller-declared skip target
            // (e.g. .kviz-widget for Visual mode's widget drag).
            if (shouldSkip(e.target)) return;
            // Suppress browser-side selection/drag for non-interactive
            // targets so the user can't accidentally drag-select clipped
            // SVG <text> labels while panning.
            try { e.preventDefault(); } catch (_) {}
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
                    // Wipe any selection that the browser may have started
                    // before the threshold was crossed; otherwise the user
                    // sees a "ghost" highlight on clipped SVG text after the
                    // pan completes.
                    try { window.getSelection()?.removeAllRanges(); } catch (_) {}
                }
                state.tx = state.panStartTx + dx;
                state.ty = state.panStartTy + dy;
                state.userAdjusted = true;
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
                state.userAdjusted = true;
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
            // Ctrl/Cmd + wheel = zoom (Figma/Miro convention). This runs even
            // when panOnWheel is set — modifier still means "zoom, not pan".
            if (e.ctrlKey || e.metaKey) {
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
                state.userAdjusted = true;
                applyTransform(state);
                return;
            }
            // Plain wheel — only handle when caller opted in (Visual mode).
            // Schematic keeps the default of "browser scroll" so its natural
            // overflow behaviour is preserved.
            if (!opts.panOnWheel) return;
            e.preventDefault();
            // Vertical wheel = vertical pan by default; Shift+wheel = horiz
            // (matches mac + Chrome desktop conventions).
            if (e.shiftKey) {
                state.tx -= e.deltaY;
            } else {
                state.tx -= e.deltaX;
                state.ty -= e.deltaY;
            }
            state.userAdjusted = true;
            applyTransform(state);
        }

        // WebKit (Safari, iOS WebView, macCatalyst BlazorWebView) fires
        // gesturestart/gesturechange/gestureend for trackpad pinch instead of
        // the synthetic wheel+ctrlKey other browsers use. Without these
        // handlers, pinching over the canvas zooms the entire WebView page
        // — the schematic appears to "pop out" of layout. We translate the
        // gesture's `scale` into the same transform state the wheel handler
        // drives, anchored at the gesture's screen midpoint.
        let gestureStartScale = 1;
        let gestureCenter = { x: 0, y: 0 };
        function onGestureStart(e) {
            if (shouldSkip(e.target)) return;
            e.preventDefault();
            gestureStartScale = state.scale;
            gestureCenter = { x: e.clientX, y: e.clientY };
        }
        function onGestureChange(e) {
            if (shouldSkip(e.target)) return;
            e.preventDefault();
            const newScale = clamp(gestureStartScale * (e.scale || 1), state.minScale, state.maxScale);
            const rect = root.getBoundingClientRect();
            const cx = gestureCenter.x - rect.left;
            const cy = gestureCenter.y - rect.top;
            const worldX = (cx - state.tx) / state.scale;
            const worldY = (cy - state.ty) / state.scale;
            state.scale = newScale;
            state.tx = cx - worldX * state.scale;
            state.ty = cy - worldY * state.scale;
            state.userAdjusted = true;
            applyTransform(state);
        }
        function onGestureEnd(e) {
            if (shouldSkip(e.target)) return;
            e.preventDefault();
        }

        function onDblClick(e) {
            if (shouldSkip(e.target)) return;
            // Quick reset on desktop — fit-to-view when that was the attach
            // strategy, otherwise plain identity. Either way the user gets a
            // known-good frame back if pan/zoom got away from them.
            if (state.fitOnAttach) {
                fitToView(state);
            } else {
                state.tx = 0; state.ty = 0; state.scale = 1;
                applyTransform(state);
            }
        }

        root.addEventListener('pointerdown', onPointerDown);
        root.addEventListener('pointermove', onPointerMove, { passive: false });
        root.addEventListener('pointerup', onPointerUp);
        root.addEventListener('pointercancel', onPointerUp);
        root.addEventListener('wheel', onWheel, { passive: false });
        root.addEventListener('dblclick', onDblClick);
        // WebKit-only events; other engines ignore the addEventListener call
        // for an unknown type and we pay nothing for the listener slot.
        root.addEventListener('gesturestart', onGestureStart, { passive: false });
        root.addEventListener('gesturechange', onGestureChange, { passive: false });
        root.addEventListener('gestureend', onGestureEnd, { passive: false });
        root.style.touchAction = 'none';
        root.__kmilaPanZoomId = id;

        // Initial framing. The schematic asks for fit-to-view because its
        // SVG renders at canvas-pixel size — on a phone the user otherwise
        // only sees the top-left corner. fitToView returns false until
        // layout has settled (e.g. dock collapsed at attach time); the
        // ResizeObserver below retries once dimensions become available.
        measureNatural(state);
        if (state.needsFit && fitToView(state)) {
            state.needsFit = false;
        }

        // ResizeObserver re-runs the clamp/fit whenever the root or target
        // change size — covers dock collapse/expand, orientation change,
        // toolbar zoom (which changes the SVG `width`/`height` attribute and
        // therefore clientWidth/Height), and waveform _zoomWidth bumps.
        let ro = null;
        try {
            ro = new ResizeObserver(() => {
                measureNatural(state);
                if (state.needsFit) {
                    if (fitToView(state)) state.needsFit = false;
                } else if (state.fitOnAttach && !state.userAdjusted) {
                    // Root changed size (dock resize, fullscreen enter/exit,
                    // orientation) and the user hasn't taken manual control —
                    // re-frame so the content stays fitted instead of locking
                    // in whatever frame the first (possibly 0-size) attach got.
                    fitToView(state);
                } else {
                    applyTransform(state);
                }
            });
            ro.observe(target);
            ro.observe(root);
        } catch (_) { /* ResizeObserver missing — clamp still works on gesture */ }

        handles.set(id, {
            state,
            teardown() {
                root.removeEventListener('pointerdown', onPointerDown);
                root.removeEventListener('pointermove', onPointerMove);
                root.removeEventListener('pointerup', onPointerUp);
                root.removeEventListener('pointercancel', onPointerUp);
                root.removeEventListener('wheel', onWheel);
                root.removeEventListener('dblclick', onDblClick);
                root.removeEventListener('gesturestart', onGestureStart);
                root.removeEventListener('gesturechange', onGestureChange);
                root.removeEventListener('gestureend', onGestureEnd);
                if (ro) { try { ro.disconnect(); } catch (_) {} }
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
        s.userAdjusted = true;
        applyTransform(s);
    }

    function reset(id) {
        const h = handles.get(id);
        if (!h) return;
        if (h.state.fitOnAttach) {
            // Schematic-style reset: re-fit + re-center. Gives users a clear
            // recovery path when toolbar "Reset" is the affordance they
            // reach for after panning into the void.
            fitToView(h.state);
        } else {
            h.state.tx = 0;
            h.state.ty = 0;
            h.state.scale = 1;
            applyTransform(h.state);
        }
    }

    // Force a re-measure of the target's natural (pre-transform) box. Callers
    // use this when the content's layout can grow beyond the last-measured
    // bounds without the target itself resizing — e.g. widgets dragged past
    // the current canvas extent. The ResizeObserver installed at attach time
    // covers the common case where the target's own client rect changes, but
    // absolutely-positioned children overflowing a fixed-size wrap don't
    // change clientWidth/Height, so callers can nudge us here after a layout
    // mutation. Cheap: one clientRect read + one transform re-apply.
    function invalidateBounds(id) {
        const h = handles.get(id);
        if (!h) return;
        measureNatural(h.state);
        applyTransform(h.state);
    }

    // §6 Item 4 (2026-07-05) — public zoom-to-fit helper for the Visual
    // Runtime's "Fit" toolbar button. Existing internal fitToView() is
    // only called at attach time when fitOnAttach is set; expose it for
    // the locator UI so users can recover a lost-in-space canvas.
    function fit(id) {
        const h = handles.get(id);
        if (!h) return;
        measureNatural(h.state);
        fitToView(h.state);
    }

    // §6 Item 4 (2026-07-05) — pan + zoom so the given target-space rect
    // lands centered inside the root viewport. Used by the locator's
    // per-widget "Focus" affordance to snap-back to a widget that scrolled
    // out of view. Coordinates are in the target's untransformed space
    // (same as widget Position.X/Y from the KvizDoc).
    function focusRect(id, x, y, w, h, opts) {
        const state = (handles.get(id) || {}).state;
        if (!state) return;
        opts = opts || {};
        const padding = opts.padding || 40;
        measureNatural(state);
        const rect = state.root.getBoundingClientRect();
        if (rect.width <= 20 || rect.height <= 20) return;
        // Fit-scale the requested rect (+ padding) into the root.
        const rw = Math.max(1, w + padding * 2);
        const rh = Math.max(1, h + padding * 2);
        const sx = rect.width  / rw;
        const sy = rect.height / rh;
        const targetScale = clamp(Math.min(sx, sy), state.minScale, state.maxScale);
        state.scale = targetScale;
        const ox = state.target.offsetLeft || 0;
        const oy = state.target.offsetTop  || 0;
        // Center of the rect maps to center of the root.
        const cx = x + w / 2;
        const cy = y + h / 2;
        state.tx = rect.width  / 2 - cx * state.scale - ox;
        state.ty = rect.height / 2 - cy * state.scale - oy;
        applyTransform(state);
    }

    window.kmilaAttachPanZoom = attach;
    window.kmilaDetachPanZoom = detach;
    window.kmilaPanZoomZoom  = zoom;
    window.kmilaPanZoomReset = reset;
    window.kmilaPanZoomInvalidateBounds = invalidateBounds;
    window.kmilaPanZoomFit    = fit;
    window.kmilaPanZoomFocus  = focusRect;

    // Utility for Blazor drag handlers that live outside the pan-zoom lib
    // (e.g. VisualCanvas widget-drag). Reads the current scale off the
    // target element's CSS transform matrix so widget pointer deltas can
    // be divided by it — otherwise a drag at 2x zoom moves twice as fast
    // as the finger. Returns 1 if the element is missing or untransformed.
    window.kmilaGetTransformScale = function (selector) {
        try {
            const el = typeof selector === 'string' ? document.querySelector(selector) : selector;
            if (!el) return 1;
            const t = getComputedStyle(el).transform;
            if (!t || t === 'none') return 1;
            const m = t.match(/matrix(3d)?\(([^)]+)\)/);
            if (!m) return 1;
            const parts = m[2].split(',').map(x => parseFloat(x));
            const sx = m[1] === '3d' ? parts[0] : parts[0];
            return (Number.isFinite(sx) && sx > 0) ? sx : 1;
        } catch (_) { return 1; }
    };
})();


/* ─── Waveform hover cursor ────────────────────────────────────────────────
 * Vertical guide line + time chip that follow the mouse over the waveform
 * body. Lets the user read off the time of any transition without having to
 * scroll down and align by eye against the ruler row.
 *
 * The line is appended into the body's content (which is the horizontal
 * scrollport) and positioned at `scrollLeft + bodyX` so as the user scrolls
 * the trace under the cursor, the line stays visually at the cursor X. The
 * chip uses the same X plus `scrollTop` for Y so it stays anchored to the
 * top of the visible body even when the body is scrolled vertically.
 *
 * Time bounds + label-column width are read live from data-* attributes on
 * the body, so Razor's re-renders (zoom changes, live-window slides, etc.)
 * naturally update what the JS computes — no JSInterop refresh needed.
 *
 * Public API:
 *   kmilaAttachWaveformHover(bodySelector) → id
 *   kmilaDetachWaveformHover(id)
 * --------------------------------------------------------------------- */
(function () {
    const handles = new Map();
    let nextId = 1;

    // Approximate chip width used for the right-edge flip decision. The chip
    // sizes itself to its text ("12.345 µs" / "10.000 ms"), so reading
    // chip.offsetWidth gives a tighter answer — but that read costs a forced
    // reflow inside the per-frame update path, which was the dominant
    // contributor to waveform-scroll jank pre-2026-06-18. A fixed 80px keeps
    // the flip threshold conservative (chip stays a few px farther from the
    // edge than strictly necessary) without paying for the layout read.
    const CHIP_FLIP_THRESHOLD_PX = 80;

    function formatTime(ns) {
        const a = Math.abs(ns);
        if (a >= 1e9) return (ns / 1e9).toFixed(3) + ' s';
        if (a >= 1e6) return (ns / 1e6).toFixed(3) + ' ms';
        if (a >= 1e3) return (ns / 1e3).toFixed(3) + ' µs';
        return Math.round(ns) + ' ns';
    }

    function attach(bodySel, dotNetRef) {
        const body = (typeof bodySel === 'string')
            ? document.querySelector(bodySel) : bodySel;
        if (!body) return -1;
        if (body.__kmilaHoverId) detach(body.__kmilaHoverId);

        // Cursor publishing: when a dotNetRef is supplied, the hover doubles as
        // the value cursor — it pushes the time under the pointer back to
        // .NET (WaveformViewer.OnCursorHover) so the value column can read each
        // signal's value at that instant. Throttled to ~60ms so a fast mouse
        // doesn't flood the Blazor circuit; only the cheap value chips
        // re-render server-side (the SVG traces are memoised).
        let lastPublishMs = 0;
        let lastPublishedNs = -1;
        const PUBLISH_THROTTLE_MS = 60;

        // Coarse-pointer (touch-primary) devices have no hover state — the
        // overlay is invisible to them and the per-event geometry math was
        // the dominant source of waveform-scroll jank on Android (perf
        // audit 2026-06-18: 19% janky frames, p90 = 61ms). Skip attaching
        // entirely so the body's only listener is the browser's own scroll.
        const mq = window.matchMedia && window.matchMedia('(pointer: coarse)');
        if (mq && mq.matches) return -1;

        const line = document.createElement('div');
        line.className = 'waveform-hover__line';
        line.style.display = 'none';
        body.appendChild(line);
        const chip = document.createElement('div');
        chip.className = 'waveform-hover__chip';
        chip.style.display = 'none';
        body.appendChild(chip);

        // Cache the grid element. It's re-created when Razor re-renders the
        // viewer (zoom, data swap), so check `.isConnected` before reusing.
        let cachedGrid = null;
        function getGrid() {
            if (cachedGrid && cachedGrid.isConnected) return cachedGrid;
            cachedGrid = body.querySelector('.waveform-grid');
            return cachedGrid;
        }

        // Latest pointer position in client coords. We resolve to body-local
        // X inside update(), which already reads the body rect — keeping a
        // raw clientX here means onMove can stay zero-allocation and one
        // assignment.
        let lastClientX = -1;
        let rafPending = false;

        function update() {
            rafPending = false;
            // --- read phase: collect every layout-affecting metric first ---
            const grid = lastClientX < 0 ? null : getGrid();
            if (lastClientX < 0 || !grid) {
                line.style.display = 'none';
                chip.style.display = 'none';
                return;
            }
            const rect    = body.getBoundingClientRect();
            const bodyX   = lastClientX - rect.left;
            const labelW  = parseFloat(body.dataset.labelWidth || '160');
            const startNs = parseFloat(body.dataset.viewStartNs || '0');
            const endNs   = parseFloat(body.dataset.viewEndNs   || '0');
            const gridW   = grid.offsetWidth;
            const gridH   = grid.offsetHeight;
            const scrollL = body.scrollLeft;
            const scrollT = body.scrollTop;

            // Out-of-bounds → hide and skip the rest.
            if (bodyX < labelW || bodyX > rect.width ||
                !(endNs > startNs) || gridW - labelW <= 0) {
                line.style.display = 'none';
                chip.style.display = 'none';
                return;
            }

            // --- compute phase: pure math, no DOM touches ---
            const trackW   = gridW - labelW;
            const trackX   = bodyX - labelW + scrollL;
            const t        = startNs + (trackX / trackW) * (endNs - startNs);
            const contentX = scrollL + bodyX;
            const flip     = (rect.width - bodyX) < CHIP_FLIP_THRESHOLD_PX;
            const chipLeft = contentX + (flip ? -CHIP_FLIP_THRESHOLD_PX : 8);

            // --- write phase: all DOM mutations grouped at the end ---
            line.style.left    = contentX + 'px';
            line.style.height  = gridH + 'px';
            line.style.display = 'block';
            chip.textContent   = formatTime(t);
            chip.style.left    = chipLeft + 'px';
            chip.style.top     = (scrollT + 4) + 'px';
            chip.style.display = 'block';

            // Publish the cursor time to .NET (throttled) so the value column
            // tracks the pointer. Guarded on both time-delta and elapsed ms so
            // a stationary pointer during scroll doesn't re-publish.
            if (dotNetRef) {
                const now = (performance && performance.now) ? performance.now() : Date.now();
                if (now - lastPublishMs >= PUBLISH_THROTTLE_MS && Math.round(t) !== lastPublishedNs) {
                    lastPublishMs = now;
                    lastPublishedNs = Math.round(t);
                    try { dotNetRef.invokeMethodAsync('OnCursorHover', t); } catch (_) {}
                }
            }
        }

        function schedule() {
            if (rafPending) return;
            rafPending = true;
            requestAnimationFrame(update);
        }

        function onMove(e)   { lastClientX = e.clientX; schedule(); }
        function onLeave()   { lastClientX = -1;       schedule(); }
        function onScroll()  { if (lastClientX >= 0)   schedule(); }

        body.addEventListener('mousemove', onMove);
        body.addEventListener('mouseleave', onLeave);
        body.addEventListener('scroll', onScroll, { passive: true });

        const id = nextId++;
        handles.set(id, { body, line, chip, onMove, onLeave, onScroll });
        body.__kmilaHoverId = id;
        return id;
    }

    function detach(id) {
        const h = handles.get(id);
        if (!h) return;
        h.body.removeEventListener('mousemove', h.onMove);
        h.body.removeEventListener('mouseleave', h.onLeave);
        h.body.removeEventListener('scroll', h.onScroll);
        try { h.line.remove(); } catch (_) {}
        try { h.chip.remove(); } catch (_) {}
        h.body.__kmilaHoverId = null;
        handles.delete(id);
    }

    window.kmilaAttachWaveformHover = attach;
    window.kmilaDetachWaveformHover = detach;
})();


/* ─── Waveform fit-to-width helper ────────────────────────────────────────
 * Returns the trace width that would make the grid (label column + trace)
 * exactly fit the visible body. The caller clamps this against the zoom
 * floor/ceiling. Returns -1 if the body isn't measurable (prerender or
 * detached); the caller should fall back to its BaseWidth in that case.
 * --------------------------------------------------------------------- */
window.kmilaWaveformFitWidth = function (bodySel, labelW) {
    const body = (typeof bodySel === 'string')
        ? document.querySelector(bodySel) : bodySel;
    if (!body) return -1;
    const w = body.clientWidth;
    if (!(w > 0)) return -1;
    // Subtract a few pixels of buffer so the body doesn't show a scrollbar
    // from sub-pixel rounding at the exact-fit boundary.
    return Math.max(0, Math.floor(w - (labelW || 160) - 2));
};

/* ─── Waveform viewport / scroll observer ─────────────────────────────────
 *
 * Used by the virtualised renderer: the .waveform-viewer__body still
 * scrolls horizontally over the FULL content width (pxPerNs × totalNs),
 * but the SVG paths inside each row only render the visible time window.
 * This module is what tells Blazor when the visible window changes —
 * scroll, resize, or zoom — so it can rebuild the windowed SVGs.
 *
 * The handler is debounced to one animation frame: scroll fires faster
 * than Blazor can render, and coalescing per-frame keeps the perf budget
 * tight on long sims with hundreds of transitions per visible window.
 *
 *   kmilaAttachWaveformWindow(bodySelector, dotNetRef) → id
 *     Subscribes to scroll + resize on the body. Calls
 *     dotNetRef.invokeMethodAsync('OnWaveformWindow', scrollLeft, clientWidth)
 *     once per RAF when either changes. Also fires once immediately so the
 *     initial render has correct values.
 *   kmilaWaveformGetViewport(bodySelector) → number
 *     One-shot read of the body's clientWidth, used for initial sizing
 *     and the Reset / fit-to-width path. -1 when the body isn't ready.
 *   kmilaDetachWaveformWindow(id) → void
 *     Cleanup. Idempotent.
 * --------------------------------------------------------------------- */
(function () {
    const handles = new Map();
    let nextId = 1;

    function attach(bodySel, dotNetRef) {
        const body = (typeof bodySel === 'string')
            ? document.querySelector(bodySel) : bodySel;
        if (!body || !dotNetRef) return -1;
        // Don't double-attach to the same body; replace if the caller
        // somehow forgot to detach (component re-mounted without dispose).
        if (body.__kmilaWindowId) detach(body.__kmilaWindowId);

        let rafPending = false;
        let lastScroll = -1;
        let lastWidth  = -1;

        function fire() {
            const s = body.scrollLeft;
            const w = body.clientWidth;
            if (s === lastScroll && w === lastWidth) return;
            lastScroll = s;
            lastWidth  = w;
            try {
                dotNetRef.invokeMethodAsync('OnWaveformWindow', s, w);
            } catch (_) {
                // Blazor side may have torn down between RAF and the
                // callback — fine, the next detach will clean us up.
            }
        }

        function schedule() {
            if (rafPending) return;
            rafPending = true;
            requestAnimationFrame(function () {
                rafPending = false;
                fire();
            });
        }

        body.addEventListener('scroll', schedule, { passive: true });

        const ro = (typeof ResizeObserver !== 'undefined')
            ? new ResizeObserver(schedule) : null;
        if (ro) ro.observe(body);

        const id = nextId++;
        handles.set(id, { body, schedule, ro });
        body.__kmilaWindowId = id;
        // Fire once on the next frame so the Blazor side has the initial
        // (scrollLeft=0, clientWidth=current) pair to size against.
        schedule();
        return id;
    }

    function detach(id) {
        const h = handles.get(id);
        if (!h) return;
        try { h.body.removeEventListener('scroll', h.schedule); } catch (_) {}
        try { if (h.ro) h.ro.disconnect(); } catch (_) {}
        h.body.__kmilaWindowId = null;
        handles.delete(id);
    }

    window.kmilaAttachWaveformWindow = attach;
    window.kmilaDetachWaveformWindow = detach;
    window.kmilaWaveformGetViewport  = function (bodySel) {
        const body = (typeof bodySel === 'string')
            ? document.querySelector(bodySel) : bodySel;
        if (!body) return -1;
        const w = body.clientWidth;
        return w > 0 ? w : -1;
    };
    // Programmatic scroll helper — used by ZoomReset and Zoom-in/out to
    // re-centre the view on the previously-visible time after pxPerNs
    // changes. Pass body.scrollLeft directly; values are clamped by the
    // browser to [0, scrollWidth - clientWidth].
    window.kmilaWaveformScrollTo = function (bodySel, leftPx) {
        const body = (typeof bodySel === 'string')
            ? document.querySelector(bodySel) : bodySel;
        if (!body) return;
        body.scrollLeft = Math.max(0, leftPx | 0);
    };
})();


/* ─── Waveform label-column resizer ────────────────────────────────────────
 * Drag handles on the right edge of the sticky signal-name column. Wiring is
 * fully client-side via ONE delegated pointerdown listener on the body: a
 * Blazor @onpointerdown would round-trip to the server before the move
 * listeners could attach, so a quick drag (and the first frames of any drag)
 * would be lost on the Server circuit. Delegation also covers grips on rows
 * added later (expanded buses, live signals) for free.
 *
 * During the drag it sets --wf-label-w + data-label-width on the body live
 * (the label cells, the sticky track offset and the hover/window helpers all
 * read them) so the column tracks the pointer with zero round-trips; on
 * release it persists the width to localStorage (a per-user pref — the Blazor
 * Server DB is a single shared file) and calls .NET once
 * (WaveformViewer.OnLabelResizeEnd) so the windowed SVG re-sizes to match.
 *
 * Public API:
 *   kmilaWaveformInitResize(bodySel, dotnetRef)  — idempotent per body element
 *   kmilaWaveformGetLabelWidth() → saved px, or 0 when none
 * --------------------------------------------------------------------- */
(function () {
    const KEY = 'kmila.waveform.labelWidth';
    const MIN = 96;
    const MAX = 480;

    function clampW(w) { return Math.max(MIN, Math.min(MAX, Math.round(w))); }

    window.kmilaWaveformGetLabelWidth = function () {
        try {
            const raw = window.localStorage.getItem(KEY);
            if (!raw) return 0;
            const n = parseInt(raw, 10);
            return Number.isFinite(n) ? clampW(n) : 0;
        } catch (_) { return 0; }
    };

    window.kmilaWaveformInitResize = function (bodySel, dotnetRef) {
        const body = (typeof bodySel === 'string')
            ? document.querySelector(bodySel) : bodySel;
        if (!body || body.__kmilaResizeInit) return;
        body.__kmilaResizeInit = true;

        body.addEventListener('pointerdown', function (ev) {
            const grip = ev.target.closest && ev.target.closest('.waveform-resize-grip');
            if (!grip || !body.contains(grip)) return;
            // Don't let the press reach the label's hide-toggle, and swallow the
            // click that follows so a grip tap never toggles a signal.
            ev.preventDefault();
            ev.stopPropagation();
            window.addEventListener('click', function suppress(ce) {
                ce.preventDefault();
                ce.stopPropagation();
            }, { capture: true, once: true });

            const startX = ev.clientX;
            const base = clampW(parseFloat(body.dataset.labelWidth || '160'));
            let next = base;

            function onMove(mv) {
                mv.preventDefault();
                next = clampW(base + (mv.clientX - startX));
                body.style.setProperty('--wf-label-w', next + 'px');
                body.dataset.labelWidth = String(next);
            }
            function onUp() {
                window.removeEventListener('pointermove', onMove);
                window.removeEventListener('pointerup', onUp);
                window.removeEventListener('pointercancel', onUp);
                document.body.classList.remove('kmila-col-resizing');
                try { window.localStorage.setItem(KEY, String(next)); } catch (_) {}
                if (dotnetRef) {
                    dotnetRef.invokeMethodAsync('OnLabelResizeEnd', next)
                        .catch(function () {});
                }
            }

            document.body.classList.add('kmila-col-resizing');
            window.addEventListener('pointermove', onMove, { passive: false });
            window.addEventListener('pointerup', onUp);
            window.addEventListener('pointercancel', onUp);
        }, { capture: true });
    };
})();


/* ─── Waveform number-base (radix) persistence ─────────────────────────────
 * Per-user preference for the global value base, kept in localStorage (the
 * Blazor Server DB is a single shared file). "" means Auto / type-default.
 * --------------------------------------------------------------------- */
(function () {
    const KEY = 'kmila.waveform.radix';
    window.kmilaWaveformGetRadix = function () {
        try { return window.localStorage.getItem(KEY) || ''; }
        catch (_) { return ''; }
    };
    window.kmilaWaveformSetRadix = function (value) {
        try {
            if (value) window.localStorage.setItem(KEY, value);
            else window.localStorage.removeItem(KEY);
        } catch (_) {}
    };
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
    if (typeof window.kmilaBroadcastEditorResize === 'function') {
        window.kmilaBroadcastEditorResize();
    }
};

/* Fire when the dock changes size (collapse toggle, snap change, drag
 * release). Chromium 124 on Android WebView doesn't always re-layout
 * nested flex descendants when a parent's padding-bottom recomputes via
 * a `body:has()` CSS-variable flip, so Monaco's ResizeObserver misses
 * the invalidation. Explicit `editor.layout()` + a synthetic window
 * resize covers Monaco and any component that listens to resize. */
window.kmilaBroadcastEditorResize = function () {
    try { window.dispatchEvent(new Event('resize')); } catch (_) {}
    if (typeof monaco !== 'undefined' && monaco.editor) {
        try {
            monaco.editor.getEditors().forEach(function (e) {
                if (e && typeof e.layout === 'function') e.layout();
            });
        } catch (_) {}
    }
};

/* Re-layout every Monaco editor after a split toggle (open OR close), where
 * the pane count changed and the surviving pane's editor is left stuck at the
 * old (half) width with a blank band beside it. Three things make this
 * reliable where a plain kmilaBroadcastEditorResize was not:
 *   1. EXPLICIT dimensions. With `automaticLayout: true`, a no-arg layout()
 *      can reuse Monaco's cached container size (the ResizeObserver misses the
 *      pane resize — the whole reason the band appears), so we measure the
 *      editor's own container and pass {width,height} to force the new size.
 *   2. PER-EDITOR try/catch. A transient editor (a pane mid-init, or a pane
 *      being disposed after unsplit) can throw on layout(); a single
 *      try/catch around the whole loop would abort before reaching the editor
 *      that actually needs resizing.
 *   3. Runs SYNCHRONOUSLY first (reading clientWidth forces the pending
 *      reflow), then again after two animation frames to catch the layout
 *      settling and the removed pane finishing its disposal. */
window.kmilaRelayoutMonacoSoon = function () {
    var run = function () {
        if (typeof monaco === 'undefined' || !monaco.editor) return;
        monaco.editor.getEditors().forEach(function (e) {
            try {
                if (!e || typeof e.layout !== 'function') return;
                var c = e.getContainerDomNode && e.getContainerDomNode();
                if (c && c.clientWidth > 0) {
                    e.layout({ width: c.clientWidth, height: c.clientHeight });
                } else {
                    e.layout();
                }
            } catch (_) {}
        });
    };
    run();
    try { requestAnimationFrame(function () { requestAnimationFrame(run); }); } catch (_) {}
};

/* Visual runtime: scroll the canvas-stack so a widget's just-opened floating
 * props card is visible. Called from VisualPanel after a widget is newly
 * selected — the card is rendered in canvas coords (1200×800), but the phone
 * viewport shows only a slice, so the card can land off-viewport when the
 * widget itself sits near an edge. This finds the widget by data-component-id,
 * then computes the union rect of the widget + its props card and scrolls the
 * canvas-stack container just enough so the union fits inside the viewport.
 * A tiny padding (12 CSS-px) keeps the edges from clipping. */
window.kmilaVisualEnsurePropsVisible = function (componentId) {
    try {
        const widget = document.querySelector('[data-component-id="' + componentId + '"]');
        if (!widget) return;
        const container = widget.closest('.visual-page__canvas-stack');
        if (!container) return;
        const props = container.querySelector('.visual-page__props');
        const widgetRect = widget.getBoundingClientRect();
        const propsRect = props ? props.getBoundingClientRect() : widgetRect;
        const cRect = container.getBoundingClientRect();

        const uLeft   = Math.min(widgetRect.left,   propsRect.left);
        const uRight  = Math.max(widgetRect.right,  propsRect.right);
        const uTop    = Math.min(widgetRect.top,    propsRect.top);
        const uBottom = Math.max(widgetRect.bottom, propsRect.bottom);
        const pad = 12;

        let dx = 0, dy = 0;
        if (uLeft < cRect.left + pad)   dx = uLeft   - (cRect.left  + pad);
        else if (uRight > cRect.right - pad)  dx = uRight  - (cRect.right - pad);
        if (uTop < cRect.top + pad)     dy = uTop    - (cRect.top   + pad);
        else if (uBottom > cRect.bottom - pad) dy = uBottom - (cRect.bottom - pad);

        if (dx !== 0 || dy !== 0) {
            container.scrollBy({ left: dx, top: dy, behavior: 'smooth' });
        }
    } catch (_) {}
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

/* ── Concepts ExecutionPlayer: pre-recorded line highlights ────────────────
 * The Concepts track plays a hardcoded execution timeline against a read-only
 * Monaco editor (see Pages/ConceptView.razor + Components/ExecutionPlayer).
 * Each step in the timeline lists the 1-based line numbers that should glow
 * "running now". We apply Monaco line decorations the same way Practice does,
 * keyed by a WeakMap so multiple players on the same page don't trample each
 * other's state.
 *
 * The editor instance is located by walking up the DOM from each Monaco
 * editor's root and looking for the `.kmila-exec-monaco` ancestor — the
 * ExecutionPlayer component sets that class on its <StandaloneCodeEditor>
 * wrapper. If no such editor is mounted yet (prerender, hybrid boot), this
 * is a no-op and the next call will succeed once Monaco has finished.   */
const _kmilaExecDecos = new WeakMap();

function _kmilaFindExecEditor() {
    if (typeof monaco === 'undefined' || !monaco.editor) return null;
    const editors = monaco.editor.getEditors();
    if (!editors || editors.length === 0) return null;
    // Skip orphaned editors. When the user navigates between Concepts lessons,
    // BlazorMonaco may leave a stale editor instance whose DOM node is no
    // longer attached to the document — picking it would route decorations to
    // a ghost and leave the visible lesson static. `dom.isConnected` is a
    // standard DOM property that returns true only for live nodes.
    return editors.find(e => {
        const dom = e.getDomNode && e.getDomNode();
        if (!dom || !dom.isConnected) return false;
        return dom.closest && dom.closest('.kmila-exec-monaco');
    }) || null;
}

window.kmilaExecHighlightLines = function (lineNumbers) {
    const editor = _kmilaFindExecEditor();
    if (!editor) return;
    const model = editor.getModel();
    if (!model) return;

    const maxLine = model.getLineCount();
    const safe = (Array.isArray(lineNumbers) ? lineNumbers : [])
        .filter(n => Number.isInteger(n) && n >= 1 && n <= maxLine);

    const newDecos = safe.map(ln => ({
        range: new monaco.Range(ln, 1, ln, 1),
        options: { isWholeLine: true, className: 'kmila-exec-highlight' }
    }));

    const prev = _kmilaExecDecos.get(editor) || [];
    const next = editor.deltaDecorations(prev, newDecos);
    _kmilaExecDecos.set(editor, next);

    // Auto-scroll the first highlighted line into view so the user always sees
    // what's executing — Monaco's reveal-line uses smooth scroll by default.
    if (safe.length > 0) {
        try { editor.revealLineInCenterIfOutsideViewport(safe[0]); } catch { }
    }
};

window.kmilaExecClearHighlights = function () {
    const editor = _kmilaFindExecEditor();
    if (!editor) return;
    const prev = _kmilaExecDecos.get(editor) || [];
    if (prev.length > 0) {
        editor.deltaDecorations(prev, []);
        _kmilaExecDecos.set(editor, []);
    }
};

/* ── Phase B+ Deep AST (follow-up): Monaco gutter line breakpoints ────────
 * Editor.razor's main Monaco (the `.editor-monaco` wrapper) lets the user
 * click on the line-numbers / glyph margin to toggle a breakpoint at that
 * line. The click handler routes the line number back to Blazor via the
 * passed-in DotNetObjectReference; Blazor in turn calls back here with the
 * current breakpoint set, and we paint a glyph decoration on each line.
 *
 * One handler per editor — re-init is idempotent (we dispose the previous
 * handler before binding a new one). Decorations live in a WeakMap keyed
 * by editor instance so multiple panes don't trample each other.            */
const _kmilaBpDecos = new WeakMap();
const _kmilaBpListeners = new WeakMap();

function _kmilaFindEditorMonaco() {
    if (typeof monaco === 'undefined' || !monaco.editor) return null;
    const editors = monaco.editor.getEditors();
    if (!editors || editors.length === 0) return null;
    return editors.find(e => {
        const dom = e.getDomNode && e.getDomNode();
        if (!dom || !dom.isConnected) return false;
        return dom.closest && dom.closest('.editor-monaco');
    }) || null;
}

window.kmilaInitMonacoBreakpoints = function (dotNetRef) {
    const editor = _kmilaFindEditorMonaco();
    if (!editor || !dotNetRef) return false;

    // Drop any previous listener — re-init must be idempotent (the Blazor
    // component re-fires this on every editor remount).
    const prev = _kmilaBpListeners.get(editor);
    if (prev && typeof prev.dispose === 'function') {
        try { prev.dispose(); } catch { }
    }

    const handler = editor.onMouseDown(e => {
        // Monaco's MouseTargetType: GUTTER_LINE_NUMBERS = 3, GUTTER_GLYPH_MARGIN = 2.
        // Either counts as a gutter click for breakpoint-toggle purposes.
        if (!e || !e.target) return;
        const t = e.target.type;
        if (t !== 2 && t !== 3) return;
        const pos = e.target.position;
        if (!pos || !pos.lineNumber) return;
        try { dotNetRef.invokeMethodAsync('ToggleBreakpoint', pos.lineNumber); }
        catch { /* dotnet ref may be disposed during teardown — fine */ }
    });
    _kmilaBpListeners.set(editor, handler);
    return true;
};

// lineNumbers: every breakpoint line. untrackedLines: the subset that maps
// to no signal assignment, so the engine can never pause there (#4). Those
// get a dimmed glyph + a warning tooltip so the user knows the breakpoint
// won't fire. untrackedMsg is the localized warning, computed Blazor-side.
window.kmilaSetMonacoBreakpoints = function (lineNumbers, untrackedLines, untrackedMsg) {
    const editor = _kmilaFindEditorMonaco();
    if (!editor) return;
    const model = editor.getModel();
    if (!model) return;

    const maxLine = model.getLineCount();
    const safe = (Array.isArray(lineNumbers) ? lineNumbers : [])
        .filter(n => Number.isInteger(n) && n >= 1 && n <= maxLine);
    const untracked = new Set((Array.isArray(untrackedLines) ? untrackedLines : [])
        .filter(n => Number.isInteger(n)));
    const warnMsg = (typeof untrackedMsg === 'string' && untrackedMsg.length > 0)
        ? untrackedMsg
        : 'No signal transitions on this line — this breakpoint will never pause.';

    const newDecos = safe.map(ln => {
        const isUntracked = untracked.has(ln);
        return {
            range: new monaco.Range(ln, 1, ln, 1),
            options: {
                isWholeLine: false,
                glyphMarginClassName: isUntracked
                    ? 'kmila-bp-glyph kmila-bp-glyph--untracked'
                    : 'kmila-bp-glyph',
                // Hover message in Monaco's native tooltip. Untracked lines
                // carry the warning; tracked lines keep the simple label.
                glyphMarginHoverMessage: {
                    value: isUntracked
                        ? '⚠ ' + warnMsg
                        : '**Breakpoint** — line ' + ln
                }
            }
        };
    });

    const prev = _kmilaBpDecos.get(editor) || [];
    const next = editor.deltaDecorations(prev, newDecos);
    _kmilaBpDecos.set(editor, next);
};

window.kmilaDisposeMonacoBreakpoints = function () {
    const editor = _kmilaFindEditorMonaco();
    if (!editor) return;
    const prev = _kmilaBpListeners.get(editor);
    if (prev && typeof prev.dispose === 'function') {
        try { prev.dispose(); } catch { }
    }
    _kmilaBpListeners.delete(editor);
    const decos = _kmilaBpDecos.get(editor) || [];
    if (decos.length > 0) {
        editor.deltaDecorations(decos, []);
        _kmilaBpDecos.set(editor, []);
    }
};

/* ── Live-step "current line" highlight on the main editor's Monaco ──────
 * Mirrors kmilaExecHighlightLines (which targets the Concepts-lesson
 * exec-player Monaco) but for the editor's main `.editor-monaco`. The
 * Editor calls this on every OnLiveStepChanged / OnLiveChanged tick
 * while a sim is paused — the user can flip between Editor / Schematic /
 * Replay tabs and the highlighted source line tells them exactly where
 * the engine is currently parked. Cleared when the engine resumes or
 * the sim ends. */
const _kmilaEditorExecDecos = new WeakMap();

window.kmilaEditorHighlightExecLines = function (lineNumbers) {
    const editor = _kmilaFindEditorMonaco();
    if (!editor) return;
    const model = editor.getModel();
    if (!model) return;

    const maxLine = model.getLineCount();
    const safe = (Array.isArray(lineNumbers) ? lineNumbers : [])
        .filter(n => Number.isInteger(n) && n >= 1 && n <= maxLine);

    const newDecos = safe.map(ln => ({
        range: new monaco.Range(ln, 1, ln, 1),
        options: { isWholeLine: true, className: 'kmila-editor-exec-line' }
    }));

    const prev = _kmilaEditorExecDecos.get(editor) || [];
    const next = editor.deltaDecorations(prev, newDecos);
    _kmilaEditorExecDecos.set(editor, next);

    // Scroll the first highlighted line into view so a user who'd scrolled
    // elsewhere in the file snaps back to where the engine is parked.
    if (safe.length > 0) {
        try { editor.revealLineInCenterIfOutsideViewport(safe[0]); } catch { }
    }
};

window.kmilaEditorClearExecHighlights = function () {
    const editor = _kmilaFindEditorMonaco();
    if (!editor) return;
    const prev = _kmilaEditorExecDecos.get(editor) || [];
    if (prev.length > 0) {
        editor.deltaDecorations(prev, []);
        _kmilaEditorExecDecos.set(editor, []);
    }
};

// =====================================================================
// Browser-Notification bridge — invoked from Kmila.Shared.Services.
// WebSimEndNotifier when a simulation finishes. Mirrors the MAUI
// notifier's contract so the Coordinator can fire-and-forget regardless
// of target. Permission is requested lazily on first call: the prompt
// has clear context ("you just finished a sim, want these going
// forward?") rather than being an out-of-context boot prompt.
//
// Most browsers suppress the OS-level banner when the tab is
// foregrounded — that's the desired behavior, because the in-app
// toast covers that case. Background / minimized tabs get the banner.
// =====================================================================
window.kmilaNotifySimEnd = async function (title, body) {
    try {
        if (typeof window.Notification === 'undefined') return; // unsupported
        let permission = Notification.permission;
        if (permission === 'default') {
            permission = await Notification.requestPermission();
        }
        if (permission !== 'granted') return;

        // tag = stable id → re-firing replaces the previous one instead
        // of stacking. Avoids notification pileups on long sessions.
        new Notification(title || 'Kmila', {
            body: body || '',
            tag: 'kmila-sim-end',
            silent: false
        });
    } catch {
        // Notification API can throw on some platforms (e.g. iOS Safari
        // when called outside a user gesture). Swallow — the in-app toast
        // already provides the foreground signal.
    }
};

// -------------------------------------------------------------------------
// Visual mode — click-outside dismissal for the floating widget props card.
// The Blazor OnCanvasPointerDown handler only catches taps that land
// directly on the .kviz-canvas element; users click plenty of other
// "outside" places (canvas-stack padding, palette background, toolbar
// gap, wire overlay above 1200×800 canvas). This capture-phase listener
// closes the props aside on any pointerdown that isn't inside the aside
// or on a widget. Widget taps stay untouched — the widget's own Blazor
// pointerdown still runs and switches selection normally.
// -------------------------------------------------------------------------
document.addEventListener('pointerdown', function (e) {
    const aside = document.querySelector('.visual-page__props');
    if (!aside) return;
    if (!e.target || !e.target.closest) return;
    if (aside.contains(e.target)) return;
    if (e.target.closest('.kviz-widget')) return;
    const closeBtn = aside.querySelector('.visual-page__props-close');
    if (closeBtn) closeBtn.click();
}, true);

// -------------------------------------------------------------------------
// Visual mode — undo/redo/delete keyboard shortcuts (desktop only).
// Delegates to the corresponding toolbar buttons by data-testid, same
// no-JSInterop pattern as the click-outside listener above. Route-
// scoped so it only fires on /visual/* and /editor/* pages (mobile
// keyboards rarely dispatch these anyway, so no attempt is made to
// hide the handler on phones).
// -------------------------------------------------------------------------
document.addEventListener('keydown', function (e) {
    const path = window.location.pathname;
    if (!path.includes('/visual/') && !path.includes('/editor/')) return;
    // Don't fire while the user is typing in a text field or Monaco.
    const t = document.activeElement;
    if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA'
              || (t.closest && t.closest('.monaco-editor')))) return;
    const ctrl = e.ctrlKey || e.metaKey;
    if (ctrl && (e.key === 'z' || e.key === 'Z')) {
        const sel = e.shiftKey ? '[data-testid="visual-redo"]' : '[data-testid="visual-undo"]';
        const btn = document.querySelector(sel);
        if (btn && !btn.disabled) { e.preventDefault(); btn.click(); }
        return;
    }
    // Ctrl+Y as an alternate redo (Windows/Linux convention).
    if (ctrl && (e.key === 'y' || e.key === 'Y')) {
        const btn = document.querySelector('[data-testid="visual-redo"]');
        if (btn && !btn.disabled) { e.preventDefault(); btn.click(); }
        return;
    }
    // Delete / Backspace nukes the selected widget(s). Only on /visual/*
    // so we don't fight Monaco's own delete inside the editor route.
    if ((e.key === 'Delete' || e.key === 'Backspace') && path.includes('/visual/')) {
        const btn = document.querySelector('[data-testid="visual-delete"]');
        if (btn && !btn.disabled) { e.preventDefault(); btn.click(); }
    }
});

// =====================================================================
// Schematic annotation cards — drag + resize (JS-delegated).
// The cards are SVG <g class="schematic-annotation"> inside the pan-zoom
// target, so they pan/zoom natively. Dragging/resizing is done in JS for
// smoothness (a Blazor Server @onpointermove round-trips and drops frames,
// see the waveform resize grip for the same rationale). We update the <g>
// transform / card rects live and commit the final geometry to .NET on
// pointer-up. Client deltas are divided by the current pan-zoom scale so a
// card tracks the cursor 1:1 at any zoom. Capture-phase listener runs before
// the canvas pan handler; .schematic-annotation is also a pan skip-target.
// =====================================================================
(function () {
    function parseTranslate(g) {
        const t = g.getAttribute('transform') || '';
        const m = /translate\(\s*(-?[\d.]+)[ ,]+(-?[\d.]+)/.exec(t);
        return m ? { x: parseFloat(m[1]), y: parseFloat(m[2]) } : { x: 0, y: 0 };
    }

    window.kmilaAttachAnnotations = function (rootSelector, dotNetRef, svgSelector) {
        const root = document.querySelector(rootSelector);
        if (!root) return 0;
        if (root._kmilaAnnDown) {
            root.removeEventListener('pointerdown', root._kmilaAnnDown, true);
        }

        let drag = null;

        function scale() {
            try {
                const s = window.kmilaGetTransformScale(svgSelector);
                return (s && s > 0.01) ? s : 1;
            } catch (_) { return 1; }
        }

        function onMove(e) {
            if (!drag) return;
            const dx = (e.clientX - drag.sx) / drag.s;
            const dy = (e.clientY - drag.sy) / drag.s;
            if (drag.mode === 'move') {
                drag.nx = drag.x0 + dx;
                drag.ny = drag.y0 + dy;
                drag.g.setAttribute('transform', 'translate(' + drag.nx + ' ' + drag.ny + ')');
            } else {
                drag.nw = Math.max(120, drag.w0 + dx);
                drag.nh = Math.max(60, drag.h0 + dy);
                const r = drag.rects;
                // r[0] = clip rect, r[1] = background rect, last = resize grip
                r[0].setAttribute('width', drag.nw); r[0].setAttribute('height', drag.nh);
                r[1].setAttribute('width', drag.nw); r[1].setAttribute('height', drag.nh);
                const grip = r[r.length - 1];
                grip.setAttribute('x', drag.nw - 14); grip.setAttribute('y', drag.nh - 14);
            }
            e.preventDefault();
        }

        function onUp() {
            window.removeEventListener('pointermove', onMove);
            window.removeEventListener('pointerup', onUp);
            window.removeEventListener('pointercancel', onUp);
            if (!drag) return;
            const d = drag; drag = null;
            try {
                if (d.mode === 'move' && d.nx !== undefined) {
                    dotNetRef.invokeMethodAsync('OnAnnotationMoved', d.id, d.nx, d.ny);
                } else if (d.mode === 'resize' && d.nw !== undefined) {
                    dotNetRef.invokeMethodAsync('OnAnnotationResized', d.id, d.nw, d.nh);
                }
            } catch (_) { /* circuit gone */ }
        }

        function onDown(e) {
            if (e.button !== undefined && e.button !== 0) return;
            const g = e.target.closest('.schematic-annotation');
            if (!g) return;
            const id = g.getAttribute('data-ann-id');
            const resize = e.target.closest('[data-ann-resize]');
            const s = scale();
            if (resize) {
                const rects = g.querySelectorAll('rect');
                const bg = rects[1] || rects[0];
                drag = {
                    mode: 'resize', g: g, id: id, s: s, rects: rects,
                    sx: e.clientX, sy: e.clientY,
                    w0: parseFloat(bg.getAttribute('width')) || 220,
                    h0: parseFloat(bg.getAttribute('height')) || 140,
                };
            } else {
                const p = parseTranslate(g);
                drag = { mode: 'move', g: g, id: id, s: s, sx: e.clientX, sy: e.clientY, x0: p.x, y0: p.y };
            }
            // Capture-phase + stopPropagation keeps the canvas from panning.
            // (No preventDefault here — it would swallow the click/dblclick that
            // drive select/edit; onMove preventDefaults once a drag is underway.)
            e.stopPropagation();
            window.addEventListener('pointermove', onMove, { passive: false });
            window.addEventListener('pointerup', onUp);
            window.addEventListener('pointercancel', onUp);
        }

        root.addEventListener('pointerdown', onDown, true);
        root._kmilaAnnDown = onDown;
        return 1;
    };

    window.kmilaDetachAnnotations = function (rootSelector) {
        const root = document.querySelector(rootSelector);
        if (root && root._kmilaAnnDown) {
            root.removeEventListener('pointerdown', root._kmilaAnnDown, true);
            root._kmilaAnnDown = null;
        }
    };
})();

// =====================================================================
// Schematic live-activity animation (Debug free-run). Driven by
// ReplayState.OnLiveRunFrame → SchematicViewer → here, ~18 Hz. We toggle
// classes directly on the existing SVG nodes (matched by data-cell /
// data-net / data-port) instead of re-rendering the Blazor component, so a
// big diagram animates smoothly without starving the circuit. Diffed against
// the previous frame so elements that stay active don't restart their glow.
// =====================================================================
(function () {
    var liveOn = new Set();
    var liveCtx = new Set();

    function elsFor(root, key) {
        var val = key.slice(2);
        try {
            if (key[0] === 'c') {
                return root.querySelectorAll('.schematic-cell[data-cell="' + CSS.escape(val) + '"]');
            }
            return root.querySelectorAll(
                '.schematic-wire[data-net="' + CSS.escape(val) + '"], ' +
                '.schematic-port[data-port="' + CSS.escape(val) + '"]');
        } catch (_) { return []; }
    }
    function toggleKey(root, key, cls, on) {
        var els = elsFor(root, key);
        for (var i = 0; i < els.length; i++) els[i].classList.toggle(cls, on);
    }

    window.kmilaSchematicActivity = function (svgSelector, focus, activeCells, activeNets, ctxCells, ctxNets) {
        var root = document.querySelector(svgSelector);
        if (!root) return;
        root.classList.toggle('is-live-focus', !!focus);

        var nextOn = new Set();
        (activeCells || []).forEach(function (id) { nextOn.add('c:' + id); });
        (activeNets || []).forEach(function (n) { nextOn.add('n:' + n); });
        var nextCtx = new Set();
        (ctxCells || []).forEach(function (id) { nextCtx.add('c:' + id); });
        (ctxNets || []).forEach(function (n) { nextCtx.add('n:' + n); });

        liveOn.forEach(function (k) { if (!nextOn.has(k)) toggleKey(root, k, 'is-live-on', false); });
        nextOn.forEach(function (k) { if (!liveOn.has(k)) toggleKey(root, k, 'is-live-on', true); });
        liveCtx.forEach(function (k) { if (!nextCtx.has(k)) toggleKey(root, k, 'is-live-context', false); });
        nextCtx.forEach(function (k) { if (!liveCtx.has(k)) toggleKey(root, k, 'is-live-context', true); });

        liveOn = nextOn;
        liveCtx = nextCtx;
    };
})();
