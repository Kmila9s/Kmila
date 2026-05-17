// =============================================================================
// Kmila VHDL language support for Monaco.
//
// Providers registered:
//   • Monarch tokenizer (syntax highlighting)
//   • Language configuration (comments, brackets, folding, indentation)
//   • Hover           — keyword/type/builtin docs + user-symbol info
//   • Completion      — keywords, types, builtins, user declarations, snippets
//   • Definition      — Ctrl+click → declaration in current file
//   • References      — Shift+F12 → all occurrences
//   • Document symbols — outline tree (entities, architectures, processes)
//   • Signature help  — parameter hints inside builtin function calls
//
// Documentation data lives in /js/vhdl-data/vhdl-data.{lang}.js. The active
// language comes from window.kmilaCurrentDocsLang (Blazor updates it whenever
// the user switches via LanguageChanger). Missing translations fall back to
// English silently.
//
// Polls for monaco because BlazorMonaco loads it asynchronously via AMD.
// =============================================================================

(function initVhdlLanguage() {
    if (typeof monaco === 'undefined') {
        setTimeout(initVhdlLanguage, 50);
        return;
    }

    // -------------------------------------------------------------------------
    // 1. Language registration + Monarch tokenizer
    // -------------------------------------------------------------------------

    monaco.languages.register({ id: 'vhdl' });

    monaco.languages.setMonarchTokensProvider('vhdl', {
        defaultToken: '',
        ignoreCase: true,

        keywords: [
            'abs', 'access', 'after', 'alias', 'all', 'and', 'architecture', 'array',
            'assert', 'attribute', 'begin', 'block', 'body', 'buffer', 'bus', 'case',
            'component', 'configuration', 'constant', 'disconnect', 'downto', 'else',
            'elsif', 'end', 'entity', 'exit', 'file', 'for', 'function', 'generate',
            'generic', 'group', 'guarded', 'if', 'impure', 'in', 'inertial', 'inout',
            'is', 'label', 'library', 'linkage', 'literal', 'loop', 'map', 'mod',
            'nand', 'new', 'next', 'nor', 'not', 'null', 'of', 'on', 'open', 'or',
            'others', 'out', 'package', 'port', 'postponed', 'procedure', 'process',
            'pure', 'range', 'record', 'register', 'reject', 'rem', 'report', 'return',
            'rol', 'ror', 'select', 'severity', 'signal', 'shared', 'sla', 'sli',
            'sra', 'srl', 'sll', 'subtype', 'then', 'to', 'transport', 'type', 'unaffected',
            'units', 'until', 'use', 'variable', 'wait', 'when', 'while', 'with',
            'xnor', 'xor'
        ],

        types: [
            'bit', 'bit_vector', 'boolean', 'character', 'integer', 'natural',
            'positive', 'real', 'string', 'time', 'std_logic', 'std_logic_vector',
            'std_ulogic', 'std_ulogic_vector', 'signed', 'unsigned', 'line', 'text',
            'side', 'width', 'severity_level'
        ],

        builtins: [
            'rising_edge', 'falling_edge', 'now', 'readline', 'writeline', 'endline',
            'endfile', 'to_integer', 'to_unsigned', 'to_signed', 'conv_integer',
            'conv_std_logic_vector', 'resize', 'shift_left', 'shift_right',
            'rotate_left', 'rotate_right', 'write', 'read'
        ],

        operators: [
            '=', '/=', '<', '<=', '>', '>=', '+', '-', '*', '/', '**', '&', ':='
        ],

        symbols: /[=><!~?:&|+\-*\/\^%]+/,
        escapes: /\\(?:[abfnrtv\\"']|x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8})/,

        tokenizer: {
            root: [
                [/--.*$/, 'comment'],
                [/\/\*/, 'comment', '@comment'],

                [/[a-zA-Z_][a-zA-Z0-9_]*/, {
                    cases: {
                        '@keywords': 'keyword',
                        '@types':    'type',
                        '@builtins': 'predefined',
                        '@default':  'identifier'
                    }
                }],

                [/\d+#[0-9a-fA-F_]+#/, 'number.hex'],
                [/\d+\.\d+([eE][\-+]?\d+)?/, 'number.float'],
                [/\d+[eE][\-+]?\d+/, 'number.float'],
                [/\d[\d_]*/, 'number'],

                [/"([^"\\]|\\.)*$/, 'string.invalid'],
                [/"/, 'string', '@string'],

                [/'[^\\']'/, 'string'],
                [/(')(@escapes)(')/, ['string', 'string.escape', 'string']],

                [/[{}()\[\]]/, '@brackets'],
                [/[<>](?!@symbols)/, '@brackets'],
                [/@symbols/, { cases: { '@operators': 'operator', '@default': '' } }],

                { include: '@whitespace' },
                [/[;,.]/, 'delimiter']
            ],

            comment: [
                [/[^\/*]+/, 'comment'],
                [/\/\*/, 'comment', '@push'],
                ["\\*/", 'comment', '@pop'],
                [/[\/*]/, 'comment']
            ],

            string: [
                [/[^\\"]+/, 'string'],
                [/@escapes/, 'string.escape'],
                [/\\./, 'string.escape.invalid'],
                [/"/, 'string', '@pop']
            ],

            whitespace: [
                [/[ \t\r\n]+/, 'white'],
                [/\/\*/, 'comment', '@comment'],
                [/--.*$/, 'comment']
            ]
        }
    });

    // -------------------------------------------------------------------------
    // 2. Language configuration (comments, brackets, folding, indentation)
    // -------------------------------------------------------------------------

    monaco.languages.setLanguageConfiguration('vhdl', {
        comments: { lineComment: '--', blockComment: ['/*', '*/'] },
        brackets: [['{', '}'], ['[', ']'], ['(', ')']],
        autoClosingPairs: [
            { open: '{', close: '}' },
            { open: '[', close: ']' },
            { open: '(', close: ')' },
            { open: '"', close: '"', notIn: ['string'] }
        ],
        surroundingPairs: [
            { open: '{', close: '}' },
            { open: '[', close: ']' },
            { open: '(', close: ')' },
            { open: '"', close: '"' }
        ],
        folding: {
            markers: {
                start: /^\s*(entity|architecture|process|if|for|while|case|package)\b/i,
                end:   /^\s*end\b/i
            }
        },
        indentationRules: {
            increaseIndentPattern: /^\s*(entity|architecture|process|if|for|while|case|begin|package)\b.*$/i,
            decreaseIndentPattern: /^\s*(end|else|elsif|when)\b.*$/i
        }
    });

    // -------------------------------------------------------------------------
    // 3. Docs lookup — resolves the active-language doc bundle.
    //    Falls back to English silently when the current language has no data.
    // -------------------------------------------------------------------------

    var EMPTY_DOCS = { keywords: {}, types: {}, builtins: {}, snippets: {}, ui: {} };

    // Shallow-merge cache keyed by language. We blend `lang` on top of `en`
    // per category so a partial translation (e.g. only `ui` populated) still
    // shows localized hover chrome while keeping English keyword bodies —
    // instead of the old behavior that threw away the whole translation when
    // a single entry was missing.
    var _docsMergedCache = {};

    function mergeDocs(base, overlay) {
        if (!overlay) return base || EMPTY_DOCS;
        var out = {};
        var cats = ['keywords', 'types', 'builtins', 'snippets', 'ui'];
        for (var i = 0; i < cats.length; i++) {
            var c = cats[i];
            out[c] = Object.assign({}, base && base[c] || {}, overlay[c] || {});
        }
        return out;
    }

    function currentDocs() {
        var lang = window.kmilaCurrentDocsLang || 'en';
        var all  = window.kmilaVhdlDocs || {};
        if (_docsMergedCache[lang]) return _docsMergedCache[lang];
        var merged = mergeDocs(all.en || EMPTY_DOCS, all[lang]);
        _docsMergedCache[lang] = merged;
        return merged;
    }

    // Blow away the memoized merge when the UI asks for a language switch so
    // hovers immediately reflect any translations that were loaded late.
    var _prevDocsLang = window.kmilaCurrentDocsLang;
    setInterval(function () {
        if (window.kmilaCurrentDocsLang !== _prevDocsLang) {
            _prevDocsLang = window.kmilaCurrentDocsLang;
            _docsMergedCache = {};
        }
    }, 250);

    function uiString(key, fallback) {
        var docs = currentDocs();
        return (docs.ui && docs.ui[key]) || fallback;
    }

    function lookupSymbol(word) {
        var w = word.toLowerCase();
        var docs = currentDocs();
        if (docs.keywords && docs.keywords[w]) return { kind: 'keyword',  entry: docs.keywords[w] };
        if (docs.types    && docs.types[w])    return { kind: 'type',     entry: docs.types[w]    };
        if (docs.builtins && docs.builtins[w]) return { kind: 'builtin',  entry: docs.builtins[w] };
        return null;
    }

    // -------------------------------------------------------------------------
    // 4. Regex-based declaration scanner (used by hover / completion /
    //    definition / outline). Not AST-accurate — good enough for a single
    //    VHDL file, which is all Kmila edits at a time. See docs note in
    //    the README about Phase B (AST-backed) if accuracy bites.
    // -------------------------------------------------------------------------

    /**
     * Scans the model once per provider call and returns:
     *   declarations : Map<name, { kind, type, direction?, line }>
     *   entities     : [{ name, line }]
     *   architectures: [{ name, of, line }]
     *   processes    : [{ name, line }]
     */
    function scan(model) {
        var text = model.getValue();
        var declarations = new Map();
        var entities     = [];
        var architectures= [];
        var processes    = [];

        function lineOfOffset(offset) {
            return model.getPositionAt(offset).lineNumber;
        }

        // signal / variable / constant (and shared variables)
        var sigRe = /^\s*(signal|variable|constant|shared\s+variable)\s+([a-zA-Z_]\w*(?:\s*,\s*[a-zA-Z_]\w*)*)\s*:\s*([^;:=]+?)\s*(?::=|;)/gim;
        var m;
        while ((m = sigRe.exec(text)) !== null) {
            var kind = m[1].toLowerCase().replace(/\s+/g, ' ');
            var names = m[2].split(',').map(function (s) { return s.trim(); });
            var type = m[3].trim();
            var line = lineOfOffset(m.index + m[0].indexOf(m[2]));
            names.forEach(function (n) {
                if (n) declarations.set(n, { kind: kind, type: type, line: line });
            });
        }

        // port declarations — scan inside `port ( ... )` blocks
        var portBlockRe = /\bport\s*\(([\s\S]*?)\)\s*;/gim;
        var block;
        while ((block = portBlockRe.exec(text)) !== null) {
            var blockStart = block.index + block[0].indexOf('(') + 1;
            var portText   = block[1];
            // Each port: `name[, name ...] : direction type` terminated by `;` or `)`
            var portLineRe = /([a-zA-Z_]\w*(?:\s*,\s*[a-zA-Z_]\w*)*)\s*:\s*(in|out|inout|buffer|linkage)\s+([^;]+?)(?=;|$)/gim;
            var pm;
            while ((pm = portLineRe.exec(portText)) !== null) {
                var pnames = pm[1].split(',').map(function (s) { return s.trim(); });
                var dir    = pm[2].toLowerCase();
                var ptype  = pm[3].trim();
                var pline  = lineOfOffset(blockStart + pm.index);
                pnames.forEach(function (n) {
                    if (n) declarations.set(n, { kind: 'port', direction: dir, type: ptype, line: pline });
                });
            }
        }

        // entity X is
        var entityRe = /^\s*entity\s+([a-zA-Z_]\w*)\s+is\b/gim;
        while ((m = entityRe.exec(text)) !== null) {
            entities.push({ name: m[1], line: lineOfOffset(m.index) });
        }

        // architecture X of Y is
        var archRe = /^\s*architecture\s+([a-zA-Z_]\w*)\s+of\s+([a-zA-Z_]\w*)/gim;
        while ((m = archRe.exec(text)) !== null) {
            architectures.push({ name: m[1], of: m[2], line: lineOfOffset(m.index) });
        }

        // labelled process: `name : process ...`
        var procRe = /^\s*([a-zA-Z_]\w*)\s*:\s*process\b/gim;
        while ((m = procRe.exec(text)) !== null) {
            processes.push({ name: m[1], line: lineOfOffset(m.index) });
        }

        return { declarations: declarations, entities: entities, architectures: architectures, processes: processes };
    }

    // -------------------------------------------------------------------------
    // 5. Hover provider — rich tooltip with description + example
    // -------------------------------------------------------------------------

    monaco.languages.registerHoverProvider('vhdl', {
        provideHover: function (model, position) {
            var word = model.getWordAtPosition(position);
            if (!word) return null;

            var range = new monaco.Range(
                position.lineNumber, word.startColumn,
                position.lineNumber, word.endColumn
            );

            // 1. User declarations in the current buffer take priority over
            //    static docs — if the user named their signal `process` they'd
            //    still get the builtin hover, but any unique name wins locally.
            var scanResult = scan(model);
            var decl = scanResult.declarations.get(word.word);
            if (decl) {
                var kindWord = decl.kind === 'port'
                    ? (uiString('kindPort', 'port') + ' (' + decl.direction + ')')
                    : (uiString('kind' + decl.kind.charAt(0).toUpperCase() + decl.kind.slice(1), decl.kind));

                return {
                    range: range,
                    contents: [
                        { value: '**' + kindWord + '** `' + word.word + '`' },
                        { value: '```vhdl\n' + decl.type + '\n```' },
                        { value: uiString('declaredOnLine', 'declared on line') + ' ' + decl.line }
                    ]
                };
            }

            // 2. Static symbol — keyword / type / builtin
            var sym = lookupSymbol(word.word);
            if (sym) {
                var contents = [];
                var catKey = sym.kind === 'keyword' ? 'categoryKeyword'
                           : sym.kind === 'type'    ? 'categoryType'
                           :                          'categoryBuiltin';
                var catLabel = uiString(catKey, sym.kind);

                contents.push({ value: '**' + word.word + '** — _' + catLabel + '_' });

                // Translation-pending notice — fires when the current language
                // isn't English AND its own docs don't carry an entry for this
                // symbol. Tells the user the lang switch *did* take effect;
                // the body is in English until someone translates it.
                var lang = window.kmilaCurrentDocsLang || 'en';
                if (lang !== 'en') {
                    var langOwn = (window.kmilaVhdlDocs || {})[lang] || {};
                    var cat = sym.kind === 'keyword' ? 'keywords'
                            : sym.kind === 'type'    ? 'types'
                            :                          'builtins';
                    if (!langOwn[cat] || !langOwn[cat][word.word.toLowerCase()]) {
                        contents.push({ value: '_' + uiString('translationPending',
                            '⚠ Translation pending — description shown in English.') + '_' });
                    }
                }

                if (sym.entry.signature) {
                    contents.push({ value: '```vhdl\n' + sym.entry.signature + '\n```' });
                }
                if (sym.entry.summary) {
                    contents.push({ value: sym.entry.summary });
                }
                if (sym.entry.description && sym.entry.description !== sym.entry.summary) {
                    contents.push({ value: sym.entry.description });
                }
                if (sym.entry.example) {
                    contents.push({ value: '**Example**\n```vhdl\n' + sym.entry.example + '\n```' });
                }

                return { range: range, contents: contents };
            }

            return null;
        }
    });

    // -------------------------------------------------------------------------
    // 6. Completion provider
    // -------------------------------------------------------------------------

    function docToMarkdown(entry, includeSignature) {
        var md = '';
        if (includeSignature && entry.signature) md += '```vhdl\n' + entry.signature + '\n```\n\n';
        if (entry.summary) md += entry.summary + '\n\n';
        if (entry.description && entry.description !== entry.summary) md += entry.description + '\n\n';
        if (entry.example) md += '```vhdl\n' + entry.example + '\n```';
        return { value: md.trim(), isTrusted: false };
    }

    monaco.languages.registerCompletionItemProvider('vhdl', {
        triggerCharacters: ['.'],
        provideCompletionItems: function (model, position) {
            var word = model.getWordUntilPosition(position);
            var range = {
                startLineNumber: position.lineNumber,
                endLineNumber:   position.lineNumber,
                startColumn:     word.startColumn,
                endColumn:       word.endColumn
            };

            var suggestions = [];
            var docs = currentDocs();

            // Keywords
            Object.keys(docs.keywords || {}).forEach(function (kw) {
                var e = docs.keywords[kw];
                suggestions.push({
                    label: kw,
                    kind: monaco.languages.CompletionItemKind.Keyword,
                    insertText: kw,
                    detail: e.summary,
                    documentation: docToMarkdown(e),
                    range: range
                });
            });

            // Types
            Object.keys(docs.types || {}).forEach(function (t) {
                var e = docs.types[t];
                suggestions.push({
                    label: t,
                    kind: monaco.languages.CompletionItemKind.Class,
                    insertText: t,
                    detail: e.summary,
                    documentation: docToMarkdown(e),
                    range: range
                });
            });

            // Builtins (functions)
            Object.keys(docs.builtins || {}).forEach(function (b) {
                var e = docs.builtins[b];
                suggestions.push({
                    label: b,
                    kind: monaco.languages.CompletionItemKind.Function,
                    insertText: b,
                    detail: e.signature || e.summary,
                    documentation: docToMarkdown(e, true),
                    range: range
                });
            });

            // Snippets
            Object.keys(docs.snippets || {}).forEach(function (key) {
                var e = docs.snippets[key];
                suggestions.push({
                    label: e.label || key,
                    kind: monaco.languages.CompletionItemKind.Snippet,
                    insertText: e.insertText,
                    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                    detail: e.summary,
                    range: range
                });
            });

            // User-scope declarations
            var scanResult = scan(model);
            scanResult.declarations.forEach(function (decl, name) {
                var kindLabel = decl.kind === 'port'
                    ? uiString('kindPort', 'port') + ' (' + decl.direction + ')'
                    : decl.kind;
                suggestions.push({
                    label: name,
                    kind: monaco.languages.CompletionItemKind.Variable,
                    insertText: name,
                    detail: kindLabel + ' : ' + decl.type,
                    documentation: {
                        value: uiString('declaredOnLine', 'declared on line') + ' ' + decl.line
                    },
                    range: range
                });
            });

            // Entity names (for instantiations)
            scanResult.entities.forEach(function (ent) {
                suggestions.push({
                    label: ent.name,
                    kind: monaco.languages.CompletionItemKind.Class,
                    insertText: ent.name,
                    detail: 'entity',
                    documentation: { value: uiString('declaredOnLine', 'declared on line') + ' ' + ent.line },
                    range: range
                });
            });

            return { suggestions: suggestions };
        }
    });

    // -------------------------------------------------------------------------
    // 7. Definition provider (Ctrl+click / F12)
    // -------------------------------------------------------------------------

    monaco.languages.registerDefinitionProvider('vhdl', {
        provideDefinition: function (model, position) {
            var word = model.getWordAtPosition(position);
            if (!word) return null;

            var scanResult = scan(model);

            // Prefer user declarations
            var decl = scanResult.declarations.get(word.word);
            if (decl) {
                return {
                    uri: model.uri,
                    range: new monaco.Range(decl.line, 1, decl.line, 1)
                };
            }

            // Entity / architecture / process names
            var ent = scanResult.entities.find(function (e) { return e.name === word.word; });
            if (ent) return { uri: model.uri, range: new monaco.Range(ent.line, 1, ent.line, 1) };

            var arch = scanResult.architectures.find(function (a) { return a.name === word.word; });
            if (arch) return { uri: model.uri, range: new monaco.Range(arch.line, 1, arch.line, 1) };

            var proc = scanResult.processes.find(function (p) { return p.name === word.word; });
            if (proc) return { uri: model.uri, range: new monaco.Range(proc.line, 1, proc.line, 1) };

            return null;
        }
    });

    // -------------------------------------------------------------------------
    // 8. References provider (Shift+F12)
    // -------------------------------------------------------------------------

    monaco.languages.registerReferenceProvider('vhdl', {
        provideReferences: function (model, position) {
            var word = model.getWordAtPosition(position);
            if (!word) return [];

            // Word-boundary, case-insensitive (VHDL is case-insensitive).
            var matches = model.findMatches(
                '\\b' + word.word + '\\b',
                /* searchOnlyEditableRange */ false,
                /* isRegex */                 true,
                /* matchCase */               false,
                /* wordSeparators */           null,
                /* captureMatches */          false
            );
            return matches.map(function (m) {
                return { uri: model.uri, range: m.range };
            });
        }
    });

    // -------------------------------------------------------------------------
    // 9. Document symbols (outline)
    // -------------------------------------------------------------------------

    monaco.languages.registerDocumentSymbolProvider('vhdl', {
        provideDocumentSymbols: function (model) {
            var scanResult = scan(model);
            var symbols = [];

            scanResult.entities.forEach(function (e) {
                symbols.push({
                    name: e.name,
                    detail: 'entity',
                    kind: monaco.languages.SymbolKind.Class,
                    range: new monaco.Range(e.line, 1, e.line, model.getLineMaxColumn(e.line)),
                    selectionRange: new monaco.Range(e.line, 1, e.line, model.getLineMaxColumn(e.line))
                });
            });

            scanResult.architectures.forEach(function (a) {
                symbols.push({
                    name: a.name + ' of ' + a.of,
                    detail: 'architecture',
                    kind: monaco.languages.SymbolKind.Module,
                    range: new monaco.Range(a.line, 1, a.line, model.getLineMaxColumn(a.line)),
                    selectionRange: new monaco.Range(a.line, 1, a.line, model.getLineMaxColumn(a.line))
                });
            });

            scanResult.processes.forEach(function (p) {
                symbols.push({
                    name: p.name,
                    detail: 'process',
                    kind: monaco.languages.SymbolKind.Function,
                    range: new monaco.Range(p.line, 1, p.line, model.getLineMaxColumn(p.line)),
                    selectionRange: new monaco.Range(p.line, 1, p.line, model.getLineMaxColumn(p.line))
                });
            });

            scanResult.declarations.forEach(function (decl, name) {
                var kind = decl.kind === 'port'         ? monaco.languages.SymbolKind.Property
                         : decl.kind === 'signal'       ? monaco.languages.SymbolKind.Variable
                         : decl.kind === 'variable'     ? monaco.languages.SymbolKind.Variable
                         : decl.kind === 'constant'     ? monaco.languages.SymbolKind.Constant
                         :                                 monaco.languages.SymbolKind.Variable;
                symbols.push({
                    name: name,
                    detail: decl.kind === 'port'
                        ? decl.direction + ' : ' + decl.type
                        : decl.type,
                    kind: kind,
                    range: new monaco.Range(decl.line, 1, decl.line, model.getLineMaxColumn(decl.line)),
                    selectionRange: new monaco.Range(decl.line, 1, decl.line, model.getLineMaxColumn(decl.line))
                });
            });

            return symbols;
        }
    });

    // -------------------------------------------------------------------------
    // 10. Signature help — parameter hints inside builtin function calls
    // -------------------------------------------------------------------------

    monaco.languages.registerSignatureHelpProvider('vhdl', {
        signatureHelpTriggerCharacters: ['(', ','],
        provideSignatureHelp: function (model, position) {
            // Look back from cursor for `<name>(` without a matching `)`
            var lineText = model.getLineContent(position.lineNumber)
                               .substring(0, position.column - 1);
            var m = lineText.match(/([a-zA-Z_]\w*)\s*\([^()]*$/);
            if (!m) return null;

            var funcName = m[1].toLowerCase();
            var docs = currentDocs();
            var info = docs.builtins && docs.builtins[funcName];
            if (!info || !info.signature) return null;

            return {
                value: {
                    signatures: [{
                        label: info.signature,
                        documentation: info.description || info.summary,
                        parameters: []
                    }],
                    activeSignature: 0,
                    activeParameter: 0
                },
                dispose: function () {}
            };
        }
    });

    // -------------------------------------------------------------------------
    // 11. Live linter — fast pattern-pass diagnostics that run on every edit.
    //
    // The C# Build path (Editor.razor → Builder.BuildAsync → Parser → exceptions
    // → kmilaSetMonacoMarkers) only fires when the student clicks Run/Build.
    // For surface-level typos that should surface immediately (unknown port
    // types, missing `;` between ports), waiting for a Build round-trip is
    // poor UX — and the round-trip itself has a long chain of failure modes
    // (Monaco→disk sync, two parser stacks, Token.Col being a token-index
    // not a char offset, JS interop scoping). Linting in JS bypasses all of
    // that: reads Monaco's authoritative buffer, emits markers with exact
    // character ranges, no .NET round-trip.
    //
    // Marker ownership: this writer uses owner "kmila-vhdl-live". The C#
    // Build path uses "kmila-vhdl". Different owners coexist on the same
    // model — Build markers persist until the next Build, live-lint markers
    // refresh on every keystroke (debounced 250 ms).
    //
    // Scope (v1): unknown port types with Levenshtein-≤-2 typo suggestions,
    // and missing `;` between port declarations. Mirrors the strictness in
    // VariableSyntetizer.cs / PortsParser.cs but client-side.
    // -------------------------------------------------------------------------

    var KMILA_KNOWN_VHDL_TYPES = [
        'std_logic', 'std_logic_vector', 'std_ulogic', 'std_ulogic_vector',
        'bit', 'bit_vector', 'boolean', 'integer', 'natural', 'positive',
        'character', 'string', 'unsigned', 'signed', 'real', 'time'
    ];

    function kmilaLevenshtein(a, b) {
        if (a === b) return 0;
        if (!a) return b.length;
        if (!b) return a.length;
        var m = a.length, n = b.length;
        var prev = new Array(n + 1);
        var curr = new Array(n + 1);
        for (var j = 0; j <= n; j++) prev[j] = j;
        for (var i = 1; i <= m; i++) {
            curr[0] = i;
            for (var k = 1; k <= n; k++) {
                var cost = a.charCodeAt(i - 1) === b.charCodeAt(k - 1) ? 0 : 1;
                curr[k] = Math.min(curr[k - 1] + 1, prev[k] + 1, prev[k - 1] + cost);
            }
            var tmp = prev; prev = curr; curr = tmp;
        }
        return prev[n];
    }

    // Returns the closest canonical type name within Levenshtein distance 2,
    // or null when no close match exists. Mirrors Constants.FindTypoSuggestion.
    function kmilaFindTypoSuggestion(unknown) {
        if (!unknown) return null;
        var lower = unknown.toLowerCase();
        if (KMILA_KNOWN_VHDL_TYPES.indexOf(lower) >= 0) return null;
        var best = null;
        var bestDist = 3; // strictly less than 3 → distance ≤ 2 → typo
        for (var i = 0; i < KMILA_KNOWN_VHDL_TYPES.length; i++) {
            var d = kmilaLevenshtein(lower, KMILA_KNOWN_VHDL_TYPES[i]);
            if (d < bestDist) { bestDist = d; best = KMILA_KNOWN_VHDL_TYPES[i]; }
        }
        return best;
    }

    // Single regex that captures the type identifier on a port-list line.
    // Matches: optional leading whitespace, comma-separated identifier(s),
    // a colon, optional direction, then the type identifier we want to lint.
    // Anything after the type (vector ranges, defaults) is ignored — we only
    // need to find the type token for the typo check.
    var KMILA_PORT_LINE_RE = /^\s*([a-zA-Z_][a-zA-Z0-9_]*(?:\s*,\s*[a-zA-Z_][a-zA-Z0-9_]*)*)\s*:\s*(?:in|out|inout|buffer)?\s+([a-zA-Z_][a-zA-Z0-9_]*)/i;

    function kmilaLintVhdl(text) {
        var markers = [];
        if (!text) return markers;

        var lines = text.split(/\r?\n/);
        var inEntity = false;
        var inPortList = false;
        var parenDepth = 0;

        for (var i = 0; i < lines.length; i++) {
            var raw = lines[i];
            // Strip line comments (--…) so commented-out typos don't fire.
            var commentIdx = raw.indexOf('--');
            var stripped = commentIdx >= 0 ? raw.substring(0, commentIdx) : raw;
            var lower = stripped.trim().toLowerCase();

            if (lower.length === 0) continue;

            if (!inEntity) {
                if (/^entity\s+[a-z_][a-z0-9_]*\s+is\b/.test(lower)) inEntity = true;
                continue;
            }
            // Inside entity: watch for end, generic, port.
            if (/^end\b/.test(lower)) {
                inEntity = false; inPortList = false; parenDepth = 0; continue;
            }
            if (!inPortList && /^port\s*\(/.test(lower)) {
                inPortList = true; parenDepth = 0;
                // Count parens on this line — `port (` opens, `);` on the
                // same line would close immediately.
                for (var c = 0; c < stripped.length; c++) {
                    if (stripped[c] === '(') parenDepth++;
                    else if (stripped[c] === ')') parenDepth--;
                }
                if (parenDepth <= 0) inPortList = false;
                continue;
            }
            if (!inPortList) continue;

            // Track paren depth on every port-list line. Once we close the
            // outer paren, the port list is done.
            for (var c2 = 0; c2 < stripped.length; c2++) {
                if (stripped[c2] === '(') parenDepth++;
                else if (stripped[c2] === ')') parenDepth--;
            }
            if (parenDepth <= 0) { inPortList = false; continue; }

            // Port-declaration line: extract type name and lint.
            var m = KMILA_PORT_LINE_RE.exec(stripped);
            if (!m) continue;
            var typeName = m[2];
            var typeNameLower = typeName.toLowerCase();

            // Don't flag generic-list direction-less syntax false positives:
            // the regex requires whitespace after `:`/direction so a malformed
            // line still has to look port-shaped to be linted.
            if (KMILA_KNOWN_VHDL_TYPES.indexOf(typeNameLower) < 0) {
                var suggestion = kmilaFindTypoSuggestion(typeName);
                if (suggestion) {
                    // Find typeName's char column in `raw` (not `stripped`) so
                    // the squiggle lands on the visible text. Search starting
                    // after `:` to avoid hitting a same-named identifier
                    // earlier on the line.
                    var colonIdx = raw.indexOf(':');
                    var typeIdx = raw.indexOf(typeName, colonIdx >= 0 ? colonIdx : 0);
                    if (typeIdx >= 0) {
                        markers.push({
                            severity: monaco.MarkerSeverity.Error,
                            startLineNumber: i + 1,
                            startColumn: typeIdx + 1,
                            endLineNumber: i + 1,
                            endColumn: typeIdx + 1 + typeName.length,
                            message: "Unknown port type '" + typeName + "'. Did you mean '" + suggestion + "'?",
                            source: 'Kmila VHDL',
                            code: 'VHD-UNKNOWN-TYPE'
                        });
                    }
                }
            }

            // Missing-`;` check: only flag when the line lacks both `;` and
            // a trailing `,` AND the next non-blank line looks like another
            // port declaration. Avoids false positives for the final port
            // (whose `;` is omitted by some styles) and for continuation
            // lines of multi-line declarations.
            var strippedRight = stripped.replace(/\s+$/, '');
            if (strippedRight.length > 0 &&
                strippedRight[strippedRight.length - 1] !== ';' &&
                strippedRight[strippedRight.length - 1] !== ',')
            {
                // Look ahead for the next non-blank, non-comment line.
                var j = i + 1;
                while (j < lines.length) {
                    var peekRaw = lines[j];
                    var peekStripped = (peekRaw.indexOf('--') >= 0
                        ? peekRaw.substring(0, peekRaw.indexOf('--'))
                        : peekRaw).trim();
                    if (peekStripped.length > 0) break;
                    j++;
                }
                if (j < lines.length) {
                    var nextLine = lines[j];
                    var nextStripped = (nextLine.indexOf('--') >= 0
                        ? nextLine.substring(0, nextLine.indexOf('--'))
                        : nextLine).trim();
                    // Heuristic: another port declaration starts with
                    // identifier(s) followed by `:`.
                    if (/^[a-zA-Z_][a-zA-Z0-9_]*(?:\s*,\s*[a-zA-Z_][a-zA-Z0-9_]*)*\s*:/.test(nextStripped)) {
                        // Mark the "missing ;" position one past the last
                        // visible character — same convention Monaco uses
                        // for missing-token errors in TS/JS. Monaco renders
                        // markers in the virtual-whitespace gutter past EOL.
                        var endCol = raw.replace(/\s+$/, '').length + 1;
                        markers.push({
                            severity: monaco.MarkerSeverity.Error,
                            startLineNumber: i + 1,
                            startColumn: endCol,
                            endLineNumber: i + 1,
                            endColumn: endCol + 1,
                            message: "Missing ';' at end of port declaration.",
                            source: 'Kmila VHDL',
                            code: 'VHD-MISSING-SEMICOLON'
                        });
                    }
                }
            }
        }

        return markers;
    }

    // Per-model debounce timers so rapid typing doesn't spam the lint pass.
    // WeakMap is keyed by model so disposal clears the entry automatically.
    var kmilaLintTimers = new WeakMap();

    function kmilaScheduleLint(model) {
        var prev = kmilaLintTimers.get(model);
        if (prev) clearTimeout(prev);
        kmilaLintTimers.set(model, setTimeout(function () {
            try {
                if (model.isDisposed && model.isDisposed()) return;
                var markers = kmilaLintVhdl(model.getValue());
                monaco.editor.setModelMarkers(model, 'kmila-vhdl-live', markers);
            } catch (e) {
                console.warn('[kmila live-lint]', e);
            }
        }, 250));
    }

    // Hook every VHDL model — both ones that already exist (BlazorMonaco may
    // create the model before vhdl.js finishes loading) and future ones.
    monaco.editor.onDidCreateModel(function (model) {
        if (model.getLanguageId && model.getLanguageId() === 'vhdl') {
            kmilaScheduleLint(model);
            model.onDidChangeContent(function () { kmilaScheduleLint(model); });
        }
    });
    var existing = monaco.editor.getModels ? monaco.editor.getModels() : [];
    for (var em = 0; em < existing.length; em++) {
        var emodel = existing[em];
        if (emodel.getLanguageId && emodel.getLanguageId() === 'vhdl') {
            kmilaScheduleLint(emodel);
            (function (mm) {
                mm.onDidChangeContent(function () { kmilaScheduleLint(mm); });
            })(emodel);
        }
    }

    // Expose the lint function for tests and ad-hoc debugging from devtools.
    window.kmilaLintVhdl = kmilaLintVhdl;

    console.log('[Kmila] VHDL language support registered (providers: hover, completion, definition, references, documentSymbols, signatureHelp, live-lint)');
})();
