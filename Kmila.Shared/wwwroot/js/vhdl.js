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

    console.log('[Kmila] VHDL language support registered (providers: hover, completion, definition, references, documentSymbols, signatureHelp)');
})();
