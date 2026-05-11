/* ============================================================================
   kmila-shared-head.js — single source of truth for HEAD-level boot logic.

   Loaded SYNCHRONOUSLY (no defer / no async) from <head> of every host page
   so it runs before <body> renders and before Blazor starts. That's required
   for the pre-FOUC theme + text-scale application to work.

   This file is shared between BOTH host targets:
     • MAUI:  Kmila/wwwroot/index.html
     • Web :  Kmila.Web/Components/App.razor

   IF YOU EDIT THIS FILE: bump the ?v= query string on the <script src>
   line in BOTH host pages so browsers re-fetch instead of serving the
   cached old version (Chromium / WebKit cache static JS aggressively).

   IF YOU NEED A NEW HEAD-LEVEL BEHAVIOR: add it here, never inline in the
   host pages. The hosts only carry target-specific bits (MAUI safe-area
   div, Web reconnect modal, etc.) — see project_dual_host_pages.md in the
   memory directory for the rationale.
   ============================================================================ */

(function () {
    "use strict";

    // -------------------------------------------------------------------------
    // 1) Theme — applied pre-FOUC so 'auto' resolves once before first paint.
    //    Bootstrap 5.3 watches data-bs-theme on <html> and re-themes itself.
    // -------------------------------------------------------------------------
    window.kmilaResolveTheme = function (input) {
        if (input === "dark" || input === "light") return input;
        try {
            return window.matchMedia("(prefers-color-scheme: dark)").matches
                ? "dark"
                : "light";
        } catch (_) {
            return "light";
        }
    };
    try {
        var t = localStorage.getItem("kmila-theme") || "auto";
        document.documentElement.setAttribute(
            "data-bs-theme",
            window.kmilaResolveTheme(t)
        );
    } catch (_) { /* localStorage may be unavailable in some embeds */ }


    // -------------------------------------------------------------------------
    // 2) Text-size scale — applied pre-FOUC so the layout doesn't flicker
    //    from the default 1.0 to the user's chosen multiplier on each load.
    //    Settings → Apariencia → Tamaño de letra persists a step name
    //    (xs/sm/md/lg/xl); the map below translates to a numeric multiplier
    //    and pokes the CSS variable that
    //    `html { font-size: calc(16px * var(--user-text-scale)) }` reads.
    //    Keep this map in sync with KmilaTextSizeSwitcher.razor.
    // -------------------------------------------------------------------------
    window.kmilaTextScaleMap = {
        xs: "0.85",
        sm: "0.92",
        md: "1",
        lg: "1.10",
        xl: "1.20"
    };
    window.kmilaApplyTextScale = function (step) {
        var v = window.kmilaTextScaleMap[step] || "1";
        document.documentElement.style.setProperty("--user-text-scale", v);
    };
    try {
        var step = localStorage.getItem("kmila-text-scale") || "md";
        window.kmilaApplyTextScale(step);
    } catch (_) { /* same caveat as above */ }


    // -------------------------------------------------------------------------
    // 3) Language pointer used by the VHDL Monaco providers for doc strings.
    //    Blazor's LanguageChanger writes via kmilaSetDocsLang(lang).
    // -------------------------------------------------------------------------
    window.kmilaSetDocsLang = function (lang) {
        window.kmilaCurrentDocsLang = lang || "en";
        try {
            localStorage.setItem("kmila-lang", window.kmilaCurrentDocsLang);
        } catch (_) {}
    };
    try {
        window.kmilaCurrentDocsLang = localStorage.getItem("kmila-lang") || "en";
    } catch (_) {
        window.kmilaCurrentDocsLang = "en";
    }


    // -------------------------------------------------------------------------
    // 4) Native <details> popovers — auto-close on outside click + Escape.
    //    Opt out per-popover with the `data-no-auto-close` attribute when
    //    the popover wraps a long-running flow you don't want collapsing.
    // -------------------------------------------------------------------------
    document.addEventListener("click", function (e) {
        document.querySelectorAll("details[open]").forEach(function (d) {
            if (d.hasAttribute("data-no-auto-close")) return;
            if (!d.contains(e.target)) d.open = false;
        });
    });
    document.addEventListener("keydown", function (e) {
        if (e.key !== "Escape") return;
        document.querySelectorAll("details[open]").forEach(function (d) {
            if (d.hasAttribute("data-no-auto-close")) return;
            d.open = false;
        });
    });


    // -------------------------------------------------------------------------
    // 5) Swallow Ctrl+S / Cmd+S on the editor route so the browser's
    //    "Save page as…" dialog never pops up. Editor.razor's OnKeyDown still
    //    fires inside Monaco and saves through our code path. Capture phase
    //    so we beat Monaco's own handler to the event.
    // -------------------------------------------------------------------------
    document.addEventListener(
        "keydown",
        function (e) {
            var isSave =
                (e.ctrlKey || e.metaKey) &&
                !e.altKey &&
                !e.shiftKey &&
                (e.key === "s" || e.key === "S" || e.code === "KeyS");
            if (!isSave) return;
            if (document.documentElement.getAttribute("data-route") !== "editor") return;
            e.preventDefault();
            e.stopPropagation();
        },
        { capture: true }
    );


    // -------------------------------------------------------------------------
    // 6) Push parser diagnostics into Monaco. Editor.razor calls this after
    //    every Build so red/yellow squiggles appear on offending lines;
    //    passing an empty array clears all Kmila-owned markers.
    // -------------------------------------------------------------------------
    window.kmilaSetMonacoMarkers = function (markers) {
        try {
            if (!window.monaco || !window.monaco.editor) return;
            var models = window.monaco.editor.getModels();
            for (var i = 0; i < models.length; i++) {
                window.monaco.editor.setModelMarkers(
                    models[i],
                    "kmila-vhdl",
                    markers || []
                );
            }
        } catch (e) {
            console.warn("[kmilaSetMonacoMarkers]", e);
        }
    };


    // -------------------------------------------------------------------------
    // 7) Pointer-driven resize for the editor dock. Razor passes its .NET
    //    ref + the starting Y; we compute new height as
    //    (startingHeight + (startingY - currentY)) — dragging UP shrinks the
    //    dock body since the grabber sits on top of it. PointerEvents
    //    unify mouse/touch/pen with one code path.
    // -------------------------------------------------------------------------
    window.kmilaDockDragStart = function (dotnetRef, startY) {
        var dockEl = document.querySelector(".kmila-dock");
        if (!dockEl) return;
        var startHeight = dockEl.getBoundingClientRect().height;
        var moved = false;
        function onMove(ev) {
            ev.preventDefault();
            var delta = startY - ev.clientY;
            if (Math.abs(delta) > 2) moved = true;
            var next = Math.max(
                160,
                Math.min(
                    window.innerHeight - 80,
                    Math.round(startHeight + delta)
                )
            );
            dotnetRef.invokeMethodAsync("OnGrabberMove", next);
        }
        function onUp() {
            window.removeEventListener("pointermove", onMove);
            window.removeEventListener("pointerup", onUp);
            window.removeEventListener("pointercancel", onUp);
            // Snap to closest preset on phones.
            if (window.matchMedia("(max-width: 768px)").matches && moved) {
                var h = dockEl.getBoundingClientRect().height;
                var vh = window.innerHeight;
                var snap =
                    h < vh * 0.25
                        ? "peek"
                        : h < vh * 0.66
                            ? "half"
                            : "full";
                if (window.kmilaSetDockSnap) window.kmilaSetDockSnap(snap);
            }
            dotnetRef.invokeMethodAsync("OnGrabberUp").catch(function () {});
            dotnetRef.dispose();
        }
        window.addEventListener("pointermove", onMove, { passive: false });
        window.addEventListener("pointerup", onUp);
        window.addEventListener("pointercancel", onUp);
    };


    // -------------------------------------------------------------------------
    // 8) Blob-based download helper. Settings uses this to export FPGA /
    //    language JSON templates without a round-trip through the file
    //    system. Returns true on success, false on failure.
    // -------------------------------------------------------------------------
    window.kmilaDownloadText = function (fileName, mime, text) {
        try {
            var blob = new Blob([text], {
                type: mime || "application/octet-stream"
            });
            var url = URL.createObjectURL(blob);
            var a = document.createElement("a");
            a.href = url;
            a.download = fileName || "kmila-download.txt";
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            setTimeout(function () {
                URL.revokeObjectURL(url);
            }, 2000);
            return true;
        } catch (e) {
            console.error("[kmilaDownloadText] failed", e);
            return false;
        }
    };

    // Binary sibling of kmilaDownloadText — accepts a base64-encoded
    // payload (cheapest way to ship a byte[] through the C#↔JS interop
    // boundary on every platform Blazor/MAUI ships) and turns it into a
    // Blob download. Used by the practice-session ZIP export.
    window.kmilaDownloadBytes = function (fileName, mime, base64) {
        try {
            var binary = atob(base64 || "");
            var len = binary.length;
            var bytes = new Uint8Array(len);
            for (var i = 0; i < len; i++) bytes[i] = binary.charCodeAt(i);
            var blob = new Blob([bytes], {
                type: mime || "application/octet-stream"
            });
            var url = URL.createObjectURL(blob);
            var a = document.createElement("a");
            a.href = url;
            a.download = fileName || "kmila-download.bin";
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            setTimeout(function () {
                URL.revokeObjectURL(url);
            }, 2000);
            return true;
        } catch (e) {
            console.error("[kmilaDownloadBytes] failed", e);
            return false;
        }
    };


    // -------------------------------------------------------------------------
    // 9) Tab drag-and-drop stubs. Real implementation lives in
    //    _content/Kmila.Shared/js/app.js (PointerEvents). The stubs below
    //    only act as a safety net during the brief moment between this
    //    script running and app.js being parsed — Blazor's first interop
    //    call shouldn't throw.
    // -------------------------------------------------------------------------
    if (typeof window.kmilaSetupTabDrag !== "function") {
        window.kmilaSetupTabDrag = function () { /* superseded by app.js */ };
    }
    if (typeof window.kmilaGetTabDrag !== "function") {
        window.kmilaGetTabDrag = function () {
            return { paneIdx: -1, tabIdx: -1 };
        };
    }
    if (typeof window.kmilaClearTabDrag !== "function") {
        window.kmilaClearTabDrag = function () {};
    }


    // -------------------------------------------------------------------------
    // 10) Rasterise an SVG to PNG via canvas, then download. `scale` lets
    //     callers hit roughly N× the embedded dimensions for crispness in
    //     print/docs (2 ≈ retina, 3 ≈ near-print). Image-from-data-URL is
    //     the only way to round-trip SVG → canvas without tainting it
    //     (works in MAUI WebView and modern Chromium/WebKit alike).
    //     Returns a Promise resolving true/false.
    // -------------------------------------------------------------------------
    window.kmilaDownloadPng = function (fileName, svgText, scale) {
        return new Promise(function (resolve) {
            try {
                var s = (typeof scale === "number" && scale > 0) ? scale : 2;
                var widthMatch = /\bwidth="(\d+)"/.exec(svgText);
                var heightMatch = /\bheight="(\d+)"/.exec(svgText);
                var w = widthMatch ? parseInt(widthMatch[1], 10) : 1200;
                var h = heightMatch ? parseInt(heightMatch[1], 10) : 400;

                var canvas = document.createElement("canvas");
                canvas.width = Math.max(1, Math.floor(w * s));
                canvas.height = Math.max(1, Math.floor(h * s));
                var ctx = canvas.getContext("2d");
                ctx.scale(s, s);

                var img = new Image();
                img.onload = function () {
                    try {
                        ctx.drawImage(img, 0, 0);
                        canvas.toBlob(function (blob) {
                            if (!blob) {
                                resolve(false);
                                return;
                            }
                            var url = URL.createObjectURL(blob);
                            var a = document.createElement("a");
                            a.href = url;
                            a.download = fileName || "kmila-waveform.png";
                            document.body.appendChild(a);
                            a.click();
                            document.body.removeChild(a);
                            setTimeout(function () {
                                URL.revokeObjectURL(url);
                            }, 2000);
                            resolve(true);
                        }, "image/png");
                    } catch (drawErr) {
                        console.error(
                            "[kmilaDownloadPng] draw/blob failed",
                            drawErr
                        );
                        resolve(false);
                    }
                };
                img.onerror = function (e) {
                    console.error("[kmilaDownloadPng] image load failed", e);
                    resolve(false);
                };
                // Blob URL beats data: URL — large SVGs (multi-thousand
                // transitions) trip the 2 MB data-URL ceiling some engines
                // impose.
                var svgBlob = new Blob([svgText], {
                    type: "image/svg+xml;charset=utf-8"
                });
                img.src = URL.createObjectURL(svgBlob);
            } catch (e) {
                console.error("[kmilaDownloadPng] failed", e);
                resolve(false);
            }
        });
    };


    // -------------------------------------------------------------------------
    // 9) Restore the saved dock snap point on first paint (mobile only).
    //
    //    The CSS already defaults the dock to 18dvh (peek) on mobile without
    //    a snap class, so the user *always* sees a usable peek state on first
    //    load — even before this JS runs. This handler only restores a
    //    previously-chosen 'half' or 'full' so reload doesn't reset the user's
    //    last sheet position.
    //
    //    The dock element is rendered by Blazor after first paint, so we wait
    //    for it via a MutationObserver instead of querying once on DOMReady.
    // -------------------------------------------------------------------------
    var _kmilaDockSnapApplied = false;
    function applySavedDockSnap() {
        if (_kmilaDockSnapApplied) return;
        if (!window.matchMedia("(max-width: 768px)").matches) return;
        var dock = document.querySelector(".kmila-dock");
        if (!dock) return;
        try {
            var saved = localStorage.getItem("kmila-dock-snap");
            if (saved === "half" || saved === "full") {
                if (window.kmilaSetDockSnap) {
                    window.kmilaSetDockSnap(saved);
                    _kmilaDockSnapApplied = true;
                }
            } else {
                // No saved value or 'peek' — CSS default already shows peek;
                // mark applied so we don't keep observing.
                _kmilaDockSnapApplied = true;
            }
        } catch (_) {
            _kmilaDockSnapApplied = true;
        }
    }

    function watchForDock() {
        applySavedDockSnap();
        if (_kmilaDockSnapApplied) return;
        var obs = new MutationObserver(function () {
            applySavedDockSnap();
            if (_kmilaDockSnapApplied) obs.disconnect();
        });
        obs.observe(document.documentElement, {
            childList: true,
            subtree: true
        });
        // Safety net: stop observing after 30s even if the dock never appears
        // (e.g. user is on a non-editor page).
        setTimeout(function () { obs.disconnect(); }, 30000);
    }

    // Re-apply on viewport breakpoint flips (e.g. iPad rotated from
    // landscape→portrait crossing 768 px). Keep this lightweight; the
    // _kmilaDockSnapApplied flag prevents redundant work.
    window.addEventListener("resize", function () {
        _kmilaDockSnapApplied = false;
        applySavedDockSnap();
    });

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", watchForDock);
    } else {
        watchForDock();
    }
})();
