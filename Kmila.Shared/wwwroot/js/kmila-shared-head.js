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
    // 3b) Page-wide pinch-zoom guard.
    //
    //     BlazorWebView on iOS / macCatalyst (WebKit) and BlazorWebView on
    //     Windows (Chromium) both inherit the browser's built-in pinch-to-
    //     zoom on the *whole document*, which turns a casual trackpad pinch
    //     over the editor or the schematic into a page-zoom that strands
    //     toolbars off-screen. We swallow those events at the document level
    //     when the target is outside any input — the SVG canvas surfaces
    //     (.schematic-viewer__body, .waveform-viewer__body, .flow-diagram__body)
    //     attach their own pan-zoom controllers in app.js, so this guard
    //     never starves them: addEventListener at document level fires AFTER
    //     the canvas listener has already run + preventDefaulted the event.
    //     We only act when the canvas DIDN'T (i.e. user pinched elsewhere).
    //
    //     Three event families to cover all engines:
    //       • WebKit  : gesturestart / gesturechange / gestureend
    //       • others  : wheel with ctrlKey (Chrome/Firefox synthesise this
    //                   for trackpad pinch)
    // -------------------------------------------------------------------------
    function _kmilaIsTextInput(el) {
        return !!(el && el.closest && el.closest("input, textarea, select, [contenteditable=true], .monaco-editor"));
    }
    function _kmilaIsCanvasSurface(el) {
        return !!(el && el.closest && el.closest(".schematic-viewer__body, .waveform-viewer__body, .flow-diagram__body"));
    }
    document.addEventListener("wheel", function (e) {
        if (!e.ctrlKey) return;
        if (_kmilaIsTextInput(e.target)) return;
        if (_kmilaIsCanvasSurface(e.target)) return; // canvas owns it
        e.preventDefault();
    }, { passive: false });
    document.addEventListener("gesturestart", function (e) {
        if (_kmilaIsTextInput(e.target)) return;
        if (_kmilaIsCanvasSurface(e.target)) return;
        e.preventDefault();
    }, { passive: false });
    document.addEventListener("gesturechange", function (e) {
        if (_kmilaIsTextInput(e.target)) return;
        if (_kmilaIsCanvasSurface(e.target)) return;
        e.preventDefault();
    }, { passive: false });


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
    // 5) Ctrl+S / Cmd+S on the editor route → save through our own code path,
    //    and never let the browser's "Save page as…" dialog pop up.
    //
    //    History: this used to just preventDefault + stopPropagation in the
    //    capture phase, on the assumption that "Editor.razor's OnKeyDown still
    //    fires inside Monaco." It does NOT — stopPropagation in the document
    //    capture phase kills the event before it ever descends to Monaco's DOM
    //    node, so Monaco's AddCommand(Ctrl+S) and BlazorMonaco's OnKeyDown both
    //    never ran and the shortcut saved nothing (desktop + web).
    //
    //    Fix: Editor.razor registers a .NET save bridge (kmilaRegisterEditorSave)
    //    on first render. We keep the capture-phase preventDefault/stopPropagation
    //    (suppresses the browser dialog and prevents a duplicate Monaco save) and
    //    drive the single save path through that bridge — which also makes the
    //    shortcut work regardless of whether the Monaco editor currently has
    //    focus. If the bridge isn't registered (route check below guards this to
    //    the editor page, where it always is after first render), the Monaco
    //    handlers still act as a fallback because we only stopPropagation once
    //    the editor route is confirmed.
    // -------------------------------------------------------------------------
    var kmilaEditorCmdRef = null;
    // Names kept for back-compat: they register/clear the Editor's DotNetRef,
    // which now serves every editor command, not just save.
    window.kmilaRegisterEditorSave = function (dotNetRef) { kmilaEditorCmdRef = dotNetRef; };
    window.kmilaUnregisterEditorSave = function () { kmilaEditorCmdRef = null; };

    // Single command entry point shared by the web/Windows keydown path below
    // AND the native macOS menu commands (Platforms/MacCatalyst/AppDelegate.cs).
    // On Mac Catalyst a ⌘-shortcut is resolved against the menu/responder chain
    // before it reaches this WKWebView's JS, and an unmatched ⌘-combo is
    // SWALLOWED (never delivered as a keydown) — which is why ⌘S/⌘N/⌘B/⌘Enter
    // need native menu items, while ⌘X/⌘C/⌘V keep working through the system
    // Edit menu. The menu commands call this. Guards to the editor route.
    window.kmilaRunEditorCommand = function (name) {
        if (document.documentElement.getAttribute("data-route") !== "editor") return false;
        if (!kmilaEditorCmdRef) return false;
        try { kmilaEditorCmdRef.invokeMethodAsync("JsRunCommand", name); return true; }
        catch (_) { return false; /* ref disposed mid-navigation — next render re-registers */ }
    };
    // Back-compat alias (older callers / earlier macOS Save menu item).
    window.kmilaTriggerSave = function () { return window.kmilaRunEditorCommand("save"); };

    // ── macOS clipboard bridge ────────────────────────────────────────────
    // Mac Catalyst's WKWebView does NOT deliver ⌘C/⌘X/⌘V/⌘A (nor the system
    // cut:/copy:/paste: edit actions) into the web content — a documented
    // platform limitation (see Monaco issue #2205, Uno #9877). So on macOS the
    // native AppDelegate handlers drive the clipboard through these helpers +
    // the system UIPasteboard instead of relying on the WebView's own clipboard.
    // Each operates on the focused Monaco editor; harmless no-ops elsewhere.
    function kmilaFocusedMonaco() {
        try {
            if (window.monaco && monaco.editor && monaco.editor.getEditors) {
                var eds = monaco.editor.getEditors();
                for (var i = 0; i < eds.length; i++) {
                    if (eds[i] && eds[i].hasTextFocus && eds[i].hasTextFocus()) return eds[i];
                }
            }
        } catch (e) { }
        return null;
    }
    // The focused plain text field (filename inputs, search boxes, modals, …),
    // if any. Clipboard works on these the same as on Monaco.
    function kmilaActiveTextInput() {
        var el = document.activeElement;
        if (el && (el.tagName === "INPUT" || el.tagName === "TEXTAREA") && !el.disabled && !el.readOnly) return el;
        return null;
    }
    // Returns the selected text (for the native side to put on the pasteboard).
    window.kmilaNativeCopy = function () {
        var inp = kmilaActiveTextInput();
        if (inp) {
            try { return (inp.value || "").substring(inp.selectionStart || 0, inp.selectionEnd || 0); }
            catch (e) { return ""; }
        }
        var ed = kmilaFocusedMonaco();
        if (ed) {
            try { var sel = ed.getSelection(); if (sel && !sel.isEmpty()) return ed.getModel().getValueInRange(sel) || ""; }
            catch (e) { }
        }
        try { var s = window.getSelection && window.getSelection().toString(); if (s) return s; } catch (e) { }
        return "";
    };
    // Returns the selected text AND deletes it from the focused field/editor.
    window.kmilaNativeCut = function () {
        var inp = kmilaActiveTextInput();
        if (inp) {
            try {
                var t = (inp.value || "").substring(inp.selectionStart || 0, inp.selectionEnd || 0);
                if (t) { inp.setRangeText("", inp.selectionStart, inp.selectionEnd, "end"); inp.dispatchEvent(new Event("input", { bubbles: true })); }
                return t;
            } catch (e) { return ""; }
        }
        var ed = kmilaFocusedMonaco();
        if (ed) {
            try {
                var sel = ed.getSelection();
                if (sel && !sel.isEmpty()) { var text = ed.getModel().getValueInRange(sel) || ""; ed.executeEdits("kmila-cut", [{ range: sel, text: "" }]); return text; }
            } catch (e) { }
        }
        return "";
    };
    // Inserts pasteboard text at the cursor (replacing any selection).
    window.kmilaNativePaste = function (text) {
        if (text == null) return false;
        var inp = kmilaActiveTextInput();
        if (inp) {
            // setRangeText + an input event so Blazor's @bind sees the change.
            try { inp.setRangeText(text, inp.selectionStart, inp.selectionEnd, "end"); inp.dispatchEvent(new Event("input", { bubbles: true })); return true; }
            catch (e) { return false; }
        }
        var ed = kmilaFocusedMonaco();
        if (ed) {
            try { ed.trigger("kmila-paste", "type", { text: text }); return true; }
            catch (e) { return false; }
        }
        return false;
    };
    window.kmilaNativeSelectAll = function () {
        var inp = kmilaActiveTextInput();
        if (inp) { try { inp.select(); return true; } catch (e) { return false; } }
        var ed = kmilaFocusedMonaco();
        if (ed) { try { ed.trigger("kmila", "editor.action.selectAll", null); return true; } catch (e) { return false; } }
        return false;
    };

    // Client-side platform probe for the keyboard-shortcuts help popover. Done
    // in JS (not the C# IFormFactor) because on the web host the server only
    // knows ITS OS, not the visitor's — and the right modifier symbol (⌘ vs
    // Ctrl) depends on the visitor's OS. Works the same inside every host's
    // WebView. `touch` flags a touch-primary device (phone/tablet) so the UI
    // can note the shortcuts need a physical keyboard there.
    window.kmilaPlatformInfo = function () {
        var uaPlat = (navigator.userAgentData && navigator.userAgentData.platform) || "";
        var p = ((uaPlat || navigator.platform || "") + " " + (navigator.userAgent || "")).toLowerCase();
        var isIOS = /iphone|ipad|ipod/.test(p) ||
                    (/mac/.test(p) && (navigator.maxTouchPoints || 0) > 1); // iPadOS poses as Mac
        var isMac = /mac/.test(p) || isIOS;      // Apple family uses ⌘
        var isAndroid = /android/.test(p);
        var isWindows = /win/.test(p);
        var coarse = false;
        try { coarse = !!(window.matchMedia && window.matchMedia("(pointer: coarse)").matches); } catch (e) { }
        return {
            mod: isMac ? "⌘" : "Ctrl",   // ⌘ on Apple, Ctrl elsewhere
            os: isMac ? "mac" : (isAndroid ? "android" : (isWindows ? "windows" : "other")),
            touch: coarse
        };
    };

    // Editor keyboard shortcuts for web/Windows, where ⌘/Ctrl combos DO reach
    // JS. macOS routes the same commands through the native menu instead. Only
    // app-level commands are intercepted here; text clipboard (⌘X/⌘C/⌘V) is
    // deliberately left to Monaco / the browser so native cut/copy/paste
    // (including Monaco's line-aware copy) keeps working.
    var KMILA_EDITOR_KEYS = {
        "KeyS":       "save",
        "KeyN":       "new",
        "KeyB":       "build",
        "KeyT":       "split",
        "Enter":      "run",
        "NumpadEnter":"run"
    };
    document.addEventListener(
        "keydown",
        function (e) {
            if ((!e.ctrlKey && !e.metaKey) || e.altKey || e.shiftKey) return;
            if (document.documentElement.getAttribute("data-route") !== "editor") return;
            var cmd = KMILA_EDITOR_KEYS[e.code];
            if (!cmd) return;
            e.preventDefault();
            e.stopPropagation();
            window.kmilaRunEditorCommand(cmd);
        },
        { capture: true }
    );


    // -------------------------------------------------------------------------
    // 6) Push parser diagnostics into Monaco. Editor.razor calls this after
    //    every Build so red/yellow squiggles appear on offending lines;
    //    passing an empty array clears all Kmila-owned markers.
    //
    //    Targets only editors mounted under `.editor-monaco` so a build that
    //    errors on the user's VHDL doesn't tag unrelated editors elsewhere on
    //    the page (Practice's `.practice-monaco`, the Concepts ExecutionPlayer
    //    under `.kmila-exec-monaco`). Filters by `dom.isConnected` so
    //    orphaned editors from prior route navigations don't get hit either
    //    — same pattern the Concepts highlight bridge uses in js/app.js.
    // -------------------------------------------------------------------------
    // 2026-07-20 Phase 23a — optional `owner` parameter so parser markers
    // (default owner `"kmila-vhdl"`, unchanged) and emitter markers
    // (`"kmila-vhdl-emit"`, published by Editor.razor's
    // PublishEmitterDiagnosticsAsync) can be cleared independently.
    // Each `setModelMarkers` call replaces ONLY that owner's marker set,
    // per Monaco contract, so the two publishers never stomp each other.
    window.kmilaSetMonacoMarkers = function (markers, owner) {
        try {
            if (!window.monaco || !window.monaco.editor) return;
            // Defensive: C# callers must wrap their array in (object) so
            // IJSRuntime's params-args binding doesn't splat it. If a caller
            // forgets, `markers` arrives as a single object — coerce to an
            // empty array rather than passing garbage to setModelMarkers.
            var safe = Array.isArray(markers) ? markers : [];
            var ownerStr = (typeof owner === "string" && owner.length > 0)
                ? owner
                : "kmila-vhdl";
            var editors = (window.monaco.editor.getEditors && window.monaco.editor.getEditors()) || [];
            var hits = 0;
            for (var i = 0; i < editors.length; i++) {
                var e = editors[i];
                var dom = e.getDomNode && e.getDomNode();
                if (!dom || !dom.isConnected) continue;
                if (!dom.closest || !dom.closest(".editor-monaco")) continue;
                var model = e.getModel && e.getModel();
                if (!model) continue;
                window.monaco.editor.setModelMarkers(model, ownerStr, safe);
                hits++;
            }
            // Fall back to the old all-models behaviour when no editor in
            // .editor-monaco was found — covers tests / future-proofing.
            // Safe because the owner string namespaces our markers so
            // unrelated models can't be permanently dirtied.
            if (hits === 0) {
                var models = window.monaco.editor.getModels();
                for (var j = 0; j < models.length; j++) {
                    window.monaco.editor.setModelMarkers(models[j], ownerStr, safe);
                }
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
        // Tracked so onUp can snap based on where the user *wanted* the dock
        // to end up, not where it currently sits. On mobile the CSS pins
        // .kmila-dock to `height: var(--dock-h) !important` (snap-class
        // driven), so getBoundingClientRect never reflects mid-drag intent.
        var lastNext = startHeight;
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
            lastNext = next;
            dotnetRef.invokeMethodAsync("OnGrabberMove", next);
        }
        function onUp() {
            window.removeEventListener("pointermove", onMove);
            window.removeEventListener("pointerup", onUp);
            window.removeEventListener("pointercancel", onUp);
            // Snap to closest preset on phones. Use lastNext (dragged intent)
            // not the rect height — see the lastNext comment above.
            if (window.matchMedia("(max-width: 768px)").matches && moved) {
                var vh = window.innerHeight;
                var snap =
                    lastNext < vh * 0.34
                        ? "peek"
                        : lastNext < vh * 0.69
                            ? "half"
                            : "full";
                if (window.kmilaSetDockSnap) window.kmilaSetDockSnap(snap);
            }
            dotnetRef.invokeMethodAsync("OnGrabberUp").catch(function () {});
            // Ref lifetime is owned by EditorDock.razor (_selfRef, disposed in
            // Dispose()). Disposing here breaks every drag after the first.
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
    // Rasterise an SVG to PNG bytes and return them as a base64 string.
    // Used by SchematicViewer/WaveformViewer PNG export, which pipes the
    // result through IFileExportService.SaveBytesAsync so the same code
    // path works on Web (anchor download) and MAUI (Documents/Kmila/PNG/).
    // Returns a Promise<string> — empty string on failure.
    window.kmilaSvgToPngBase64 = function (svgText, scale) {
        return new Promise(function (resolve) {
            try {
                var s = (typeof scale === "number" && scale > 0) ? scale : 2;
                // Match decimal widths — SchematicExporter emits "0.##"
                // formatted numbers with a fractional part (e.g. 393.5).
                // The prior `\d+` regex silently dropped the fraction,
                // producing a canvas shorter than the SVG and leaving
                // the background rect uncovered at the bottom.
                var widthMatch = /\bwidth="(\d+(?:\.\d+)?)"/.exec(svgText);
                var heightMatch = /\bheight="(\d+(?:\.\d+)?)"/.exec(svgText);
                var w = widthMatch ? parseFloat(widthMatch[1]) : 1200;
                var h = heightMatch ? parseFloat(heightMatch[1]) : 400;

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
                            if (!blob) { resolve(""); return; }
                            var reader = new FileReader();
                            reader.onloadend = function () {
                                var result = reader.result || "";
                                var comma = result.indexOf(",");
                                resolve(comma >= 0 ? result.substring(comma + 1) : "");
                            };
                            reader.onerror = function () { resolve(""); };
                            reader.readAsDataURL(blob);
                        }, "image/png");
                    } catch (drawErr) {
                        console.error("[kmilaSvgToPngBase64] draw failed", drawErr);
                        resolve("");
                    }
                };
                img.onerror = function (e) {
                    console.error("[kmilaSvgToPngBase64] image load failed", e);
                    resolve("");
                };
                var svgBlob = new Blob([svgText], {
                    type: "image/svg+xml;charset=utf-8"
                });
                img.src = URL.createObjectURL(svgBlob);
            } catch (e) {
                console.error("[kmilaSvgToPngBase64] failed", e);
                resolve("");
            }
        });
    };

    window.kmilaDownloadPng = function (fileName, svgText, scale) {
        return new Promise(function (resolve) {
            try {
                var s = (typeof scale === "number" && scale > 0) ? scale : 2;
                // Match decimal widths — SchematicExporter emits "0.##"
                // formatted numbers with a fractional part (e.g. 393.5).
                // The prior `\d+` regex silently dropped the fraction,
                // producing a canvas shorter than the SVG and leaving
                // the background rect uncovered at the bottom.
                var widthMatch = /\bwidth="(\d+(?:\.\d+)?)"/.exec(svgText);
                var heightMatch = /\bheight="(\d+(?:\.\d+)?)"/.exec(svgText);
                var w = widthMatch ? parseFloat(widthMatch[1]) : 1200;
                var h = heightMatch ? parseFloat(heightMatch[1]) : 400;

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
    //    The CSS now defaults the dock to half-sheet (50dvh) on mobile so a
    //    first-time visitor sees panel content immediately. This handler
    //    restores a previously-chosen 'peek' / 'half' / 'full' so reload
    //    doesn't reset the user's last sheet position.
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
            if (saved === "peek" || saved === "half" || saved === "full") {
                if (window.kmilaSetDockSnap) {
                    window.kmilaSetDockSnap(saved);
                    _kmilaDockSnapApplied = true;
                }
            } else {
                // No saved value — CSS default (half) already shows content;
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


    // -------------------------------------------------------------------------
    // 6) KmilaTooltip anchor-position sync (v=6, 2026-07-19 Phase 3 followup).
    //
    // The KmilaTooltip bubble is rendered as a ::after pseudo-element on the
    // wrapper span. Before this hook, that pseudo was position:absolute and
    // therefore (a) inherited every ancestor stacking context and (b) got
    // clipped by any ancestor with overflow:hidden — hitting BOTH failure
    // modes on the same site: e.g. VisualPanel's Save button, whose tooltip
    // rendered "behind the canvas card" because .visual-page__toolbar had
    // overflow-x:auto (which implicitly clips y too) and the canvas parent
    // owns a competing stacking context.
    //
    // Fix: switch the bubble to position:fixed (see pages.css .kmila-tooltip
    // block) and drive its top/left from CSS custom properties that we
    // refresh here whenever pointer or keyboard focus reaches a wrapper.
    // position:fixed anchors to the viewport, escaping BOTH stacking-context
    // and overflow-clip constraints, with a z-index high enough to always
    // win against Blazor/Bootstrap modals.
    //
    // Delegated on document via mouseover/focusin (both bubble) so a single
    // pair of listeners covers every wrapper on the page, present or future,
    // with no per-instance JS interop required from Blazor.
    // -------------------------------------------------------------------------
    function _kmilaTooltipAnchorSync(target) {
        if (!(target instanceof Element)) return;
        var tip = target.closest ? target.closest(".kmila-tooltip") : null;
        if (!tip) return;
        // The wrapper is display: contents (v=30 followup: fixes widget
        // palette chips not stretching), so it has no box of its own and
        // getBoundingClientRect() on it returns zeros. Use the first real
        // child (the wrapped button in the canonical case) as the anchor
        // instead — its rect is what we want the bubble to point at anyway.
        var anchor = tip.firstElementChild || tip;
        var r = anchor.getBoundingClientRect();
        // A wrapper still detached from the visible layout (hidden ancestor,
        // Blazor not yet rendered) reports a zero-size rect — skip so the
        // pseudo keeps its last known coords instead of jumping to (0,0).
        if (r.width === 0 && r.height === 0) return;
        var s = tip.style;
        s.setProperty("--kt-l", r.left + "px");
        s.setProperty("--kt-r", r.right + "px");
        s.setProperty("--kt-t", r.top + "px");
        s.setProperty("--kt-b", r.bottom + "px");
        s.setProperty("--kt-w", r.width + "px");
        s.setProperty("--kt-h", r.height + "px");
    }
    document.addEventListener("mouseover", function (e) {
        _kmilaTooltipAnchorSync(e.target);
    });
    document.addEventListener("focusin", function (e) {
        _kmilaTooltipAnchorSync(e.target);
    });

    // -------------------------------------------------------------------------
    // NAV-1 (2026-07-24) — Android hardware back key handler.
    //
    // MainActivity.OnBackPressed calls this function via
    // WebView.EvaluateJavascript. Returning `true` consumes the back
    // press; `false` lets MainActivity finish the activity (exit app).
    //
    // Priority order for "close the topmost open thing":
    //   1. Any visible modal (BlazorBootstrap `.modal.show` OR custom
    //      `.kviz-wiring-modal` / `.examples-gallery-modal` etc.):
    //      click its close/done button if we can find one; else dispatch
    //      Escape at the modal so any component listening closes.
    //   2. Any `<details open>` popover in the top bar / profile menu.
    //   3. Any Bootstrap dropdown (`.dropdown-menu.show`).
    //   4. Browser history has entries → `window.history.back()`.
    //   5. Nothing left → return false (MainActivity exits).
    //
    // Only wired on MAUI (MainActivity is the only caller); web builds
    // never invoke this — the hardware back key doesn't exist there.
    // -------------------------------------------------------------------------

    function _kmilaModalTakesSpace(el) {
        // `.offsetParent !== null` is spec-null for `position: fixed` elements,
        // which is what every BB Modal is — that broke the earlier visibility
        // gate (F-086). Use the rect instead: any element with a non-zero box
        // is on-screen for our purposes.
        if (!el) return false;
        var r = el.getBoundingClientRect();
        return r.width > 0 && r.height > 0;
    }

    function _kmilaVisibleModal() {
        // BB Modal wraps content in `.modal.show` (or `.modal.fade.show`).
        var bb = document.querySelector(".modal.show, .modal.fade.show");
        if (_kmilaModalTakesSpace(bb)) return bb;
        // Custom modals — VisualWiringModal + ExamplesGallery — mount their
        // .modal element only while open, so any visible one qualifies.
        var custom = document.querySelector(
            ".kviz-wiring-modal, .examples-gallery-modal, .kmila-template-modal"
        );
        if (_kmilaModalTakesSpace(custom)) return custom;
        return null;
    }

    function _kmilaCloseModal(modal) {
        // Try any close-button marker in a language-agnostic order. F-085:
        // the previous `[aria-label="Close"|"Cerrar"]` fallback missed the
        // localised aria-label rendered by `@Traductor.T("COMMON_CLOSE")` in
        // AR/DE/FR/CH/JP. `[data-kmila-modal-close]` is a stable marker put
        // on custom-modal close buttons for exactly this reason.
        var btn = modal.querySelector(
            '[data-bs-dismiss="modal"], [data-testid="visual-wiring-done"], [data-kmila-modal-close], .kmila-modal__close'
        );
        if (btn) { try { btn.click(); return true; } catch (e) { /* fall through */ } }
        // Fallback — dispatch Escape at the modal. BB Modal + most
        // components listen for keydown Escape. Custom Blazor modals that
        // don't listen for Escape should carry `[data-kmila-modal-close]`
        // (see above) so this branch stays a last resort.
        try {
            var evt = new KeyboardEvent("keydown", {
                key: "Escape", code: "Escape", keyCode: 27, which: 27,
                bubbles: true, cancelable: true
            });
            modal.dispatchEvent(evt);
            return true;
        } catch (e) { return false; }
    }

    window.kmilaHandleAndroidBack = function () {
        try {
            // 1) Modal (visible OVER fullscreen viewer or standalone).
            var modal = _kmilaVisibleModal();
            if (modal && _kmilaCloseModal(modal)) return true;

            // 1.5) Fullscreen SchematicViewer / FlowDiagramViewer (Z10).
            //      Click the same toggle button the user's own toolbar
            //      exposes so Blazor state stays in sync (a raw
            //      classList.remove would leave `_isFullscreen == true`
            //      and the next user toggle would misbehave).
            var fs = document.querySelector(
                ".schematic-viewer.is-fullscreen, .flow-diagram.is-fullscreen"
            );
            if (fs) {
                var toggle = fs.querySelector("[data-kmila-fullscreen-toggle]");
                if (toggle) { try { toggle.click(); return true; } catch (e) { /* fall through */ } }
                fs.classList.remove("is-fullscreen");
                return true;
            }

            // 2) Any open <details> popover (ProfileMenu, FPGA picker,
            //    file-menu burger, etc. — all use <details><summary>).
            var openDetails = document.querySelector("details[open]");
            if (openDetails) {
                openDetails.removeAttribute("open");
                return true;
            }

            // 3) Bootstrap dropdown.
            var dropdown = document.querySelector(".dropdown-menu.show");
            if (dropdown) {
                var trigger = dropdown.closest(".dropdown")?.querySelector('[data-bs-toggle="dropdown"]');
                if (trigger) { try { trigger.click(); return true; } catch (e) { /* fall through */ } }
                dropdown.classList.remove("show");
                return true;
            }

            // 4) Router history — pop one entry back through Blazor's
            //    NavigationManager (which listens to popstate).
            if (window.history && window.history.length > 1) {
                window.history.back();
                return true;
            }

            // 5) Nothing to do — let the activity finish.
            return false;
        } catch (e) {
            // On any handler error, don't trap the user in the app —
            // let the OS default fire.
            return false;
        }
    };


    // -------------------------------------------------------------------------
    // Z12) visualViewport sync — publish keyboard-aware viewport metrics.
    //
    // Neither `100vh` nor `100dvh` shrink when a soft keyboard opens — both
    // are keyboard-INCLUSIVE flavors of "layout viewport". Only
    // `window.visualViewport.height` reflects the shrinkage, and CSS has no
    // native way to read it. So this section listens to visualViewport
    // resize/scroll events and mirrors two hooks onto <html> that CSS can
    // gate off:
    //   • CSS var  `--kmila-viewport-h` — the visible viewport height in px
    //   • attribute `data-kmila-kbd="open"` — set when visible < layout by ≥
    //     150 px (Android URL-bar collapse is 60-80 px, which stays below
    //     the threshold; real IMEs consume 35-55 % of viewport so always
    //     cross it).
    //
    // Any CSS rule that needs to respect the keyboard cap should use these,
    // e.g. `max-height: min(85dvh, var(--kmila-viewport-h, 100dvh) - 32px)`
    // or `html[data-kmila-kbd="open"] .kmila-dock { display: none }`.
    //
    // Fallback path: browsers without visualViewport (very old WebKit) use
    // `resize` + `orientationchange` writing innerHeight directly, so the
    // CSS var still tracks the layout viewport at minimum.
    // -------------------------------------------------------------------------
    window.__kmilaViewportSync = function () {
        try {
            var doc = document.documentElement;
            var vv = window.visualViewport;
            var winH = window.innerHeight || doc.clientHeight;
            var vpH = vv ? vv.height : winH;
            doc.style.setProperty("--kmila-viewport-h", vpH + "px");
            if (winH - vpH > 150) {
                doc.setAttribute("data-kmila-kbd", "open");
            } else {
                doc.removeAttribute("data-kmila-kbd");
            }
        } catch (_) { /* silently no-op; layout still works without the hook */ }
    };
    if (window.visualViewport) {
        window.visualViewport.addEventListener("resize", window.__kmilaViewportSync, { passive: true });
        window.visualViewport.addEventListener("scroll", window.__kmilaViewportSync, { passive: true });
    } else {
        window.addEventListener("resize", window.__kmilaViewportSync, { passive: true });
        window.addEventListener("orientationchange", window.__kmilaViewportSync, { passive: true });
    }
    window.__kmilaViewportSync();


    // -------------------------------------------------------------------------
    // Z13) Mirror `.kmila-shell[dir]` onto <html>[dir].
    //
    // MainLayout.razor:41 writes `dir="rtl"` onto `.kmila-shell` when
    // Traductor.IsRtl is true (i.e. current language is Arabic). Bootstrap's
    // bundled JS however keys off `document.documentElement.dir` (see the
    // vendored `bootstrap.bundle.min.js` around `p=()=>"rtl"===document.documentElement.dir`)
    // — without a bridge, Bootstrap's own RTL branch NEVER activates: its
    // utility classes (.ms-*, .me-*, dropdown chevrons, popover placement,
    // form-control padding, close-button alignment) all stay LTR even when
    // the Kmila descendants inside `.kmila-shell` correctly flip. Bubbling
    // the attribute one node up unlocks the vendored library's ~100 built-in
    // RTL adjustments at no CSS cost.
    //
    // Implementation: a single document-wide subtree observer filtered to
    // `dir` attribute mutations. This survives Blazor's SSR → interactive
    // rehydration (the shell node gets replaced between phases, so a direct
    // observer attached to the SSR node would leak to a detached element).
    // The callback checks target.matches(".kmila-shell") and short-circuits
    // otherwise. Idempotent: same-value writes are guarded.
    // -------------------------------------------------------------------------
    // F-083 (R9 2026-08-05, CDP-discovered): the SSR `<html lang="en">` in
    // `Kmila/wwwroot/index.html` stays "en" for every language because there's
    // no bridge from Traductor.Lang to the document element. Screen readers,
    // browser Translate, Chrome's built-in spell-check, and hyphenation all
    // key off `<html>[lang]` — being wrong on Arabic content is a real
    // accessibility regression. Piggyback on the existing dir mirror.
    var __kmilaMirrorShellDir = function () {
        var shell = document.querySelector(".kmila-shell");
        if (!shell) return;
        var d = shell.getAttribute("dir") || "ltr";
        if (document.documentElement.getAttribute("dir") !== d) {
            document.documentElement.setAttribute("dir", d);
        }
        var l = shell.getAttribute("lang");
        if (l && document.documentElement.getAttribute("lang") !== l) {
            document.documentElement.setAttribute("lang", l);
        }
    };
    var __kmilaStartDirObserver = function () {
        new MutationObserver(function (muts) {
            for (var i = 0; i < muts.length; i++) {
                var t = muts[i].target;
                // childList mutations bubble as target=parent; attribute
                // mutations bubble as target=changed-element. Either can
                // signal a shell dir/lang change (shell mounted OR shell's
                // own attribute flipped), so we just re-query and mirror.
                if (t && (t.classList && t.classList.contains("kmila-shell")
                          || muts[i].type === "childList")) {
                    __kmilaMirrorShellDir();
                    return;
                }
            }
        }).observe(document.documentElement, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ["dir", "lang"]
        });
        __kmilaMirrorShellDir();
    };
    if (document.body) __kmilaStartDirObserver();
    else document.addEventListener("DOMContentLoaded", __kmilaStartDirObserver, { once: true });
})();
