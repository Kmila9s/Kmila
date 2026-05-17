// =============================================================================
// Kmila lesson-code colorizer.
//
// Markdig emits `<pre><code class="language-vhdl">…</code></pre>` for fenced
// code blocks. By default that renders as plain text. This module asks Monaco
// to tokenize each block in place (monaco.editor.colorizeElement) so the
// snippet looks visually identical to the Editor — same tokenizer, same theme
// — without spawning a full editor instance per block.
//
// Why not full read-only Monaco editors?
//   • Memory: a lesson with N code blocks would spawn N editors.
//   • UX: read-only editors still grab tab/scroll focus and render a cursor.
//   • Layout: editors need fixed/auto-sized heights; <pre> reflows naturally.
//
// Loaded after vhdl.js so the `vhdl` language is registered by the time any
// page calls `kmilaColorizeLessonCode`. Polls for monaco the same way vhdl.js
// does, since BlazorMonaco's loader is AMD-async on first paint.
// =============================================================================

(function initLessonCodeColorizer() {

    // Map Markdig's `language-foo` class to a Monaco mime type. Anything not
    // listed falls through to plain text (we skip those — no point colorizing
    // a generic block).
    var LANG_TO_MIME = {
        'vhdl': 'vhdl',
        'verilog': 'verilog',
        'json': 'application/json',
        'js': 'text/javascript',
        'javascript': 'text/javascript',
        'ts': 'text/typescript',
        'typescript': 'text/typescript',
        'css': 'text/css',
        'html': 'text/html',
        'xml': 'application/xml',
        'sh': 'text/x-shellscript',
        'bash': 'text/x-shellscript'
    };

    function pickMonacoTheme() {
        var bs = document.documentElement.getAttribute('data-bs-theme');
        return bs === 'dark' ? 'vs-dark' : 'vs';
    }

    function colorizeOne(codeEl, theme) {
        // Skip mermaid — those got rewritten by MarkdownRenderer.cs into
        // <pre class="mermaid"> before Markdig ever emitted a `<code>` for
        // them. Belt-and-suspenders for the (rare) raw fenced case.
        if (codeEl.classList.contains('language-mermaid')) return;

        // Idempotency: Blazor may keep the same DOM nodes across re-renders
        // when only sibling state changed. Don't re-tokenize unchanged blocks.
        if (codeEl.dataset.kmilaColorized === theme) return;

        var lang = null;
        codeEl.classList.forEach(function (c) {
            if (c.indexOf('language-') === 0) lang = c.substring('language-'.length);
        });
        var mime = lang ? LANG_TO_MIME[lang.toLowerCase()] : null;
        if (!mime) return; // untagged or unsupported — leave as plain text

        monaco.editor.colorizeElement(codeEl, {
            mimeType: mime,
            theme: theme,
            tabSize: 2
        }).then(function () {
            codeEl.dataset.kmilaColorized = theme;
        }).catch(function () {
            // Tokenizer race or torn-down element — fine, keep plain text.
        });
    }

    /**
     * Colorize every fenced code block under `rootSelector` (default
     * `.lesson-markdown`). Safe to call repeatedly — already-colorized
     * blocks are skipped via a data attribute. If Monaco hasn't finished
     * loading yet (AMD loader is async even though editor.main.js is a
     * <script> tag), the call retries with a short backoff so the first
     * lesson load on a cold start still ends up colorized.
     */
    window.kmilaColorizeLessonCode = function (rootSelector, _attempt) {
        if (typeof monaco === 'undefined' || !monaco.editor || !monaco.editor.colorizeElement) {
            // Retry while monaco bootstraps — cap at ~10s of polling.
            var attempt = (_attempt || 0) + 1;
            if (attempt < 50) {
                setTimeout(function () { window.kmilaColorizeLessonCode(rootSelector, attempt); }, 200);
            }
            return;
        }
        var theme = pickMonacoTheme();
        var roots = document.querySelectorAll(rootSelector || '.lesson-markdown');
        roots.forEach(function (root) {
            root.querySelectorAll('pre > code[class*="language-"]').forEach(function (codeEl) {
                colorizeOne(codeEl, theme);
            });
        });
    };

    // Re-colorize on theme switch. Bootstrap's data-bs-theme observer
    // (kmila-shared-head.js) flips the attribute; we react by clearing the
    // colorized marker and re-running. Cheap — colorize is a synchronous
    // tokenizer pass and N is small per page.
    var themeObserver = new MutationObserver(function (mutations) {
        for (var i = 0; i < mutations.length; i++) {
            if (mutations[i].attributeName === 'data-bs-theme') {
                document.querySelectorAll('code[data-kmila-colorized]').forEach(function (el) {
                    delete el.dataset.kmilaColorized;
                });
                window.kmilaColorizeLessonCode();
                return;
            }
        }
    });
    try {
        themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme'] });
    } catch { /* prerender / non-DOM env — fine */ }
})();
