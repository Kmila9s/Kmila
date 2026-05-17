/*
 * Kmila Concepts — custom Blockly block definitions + JS interop bridge.
 *
 * Loaded AFTER blockly.min.js (see index.html / App.razor). Registers all
 * the block types our four scratch-style exercises need:
 *
 *   E1 / E2 (recipe-style):
 *     kmila_recipe_step  — stack block with a text field; drag in order.
 *
 *   E3 (half-adder, no clock):
 *     kmila_signal_in    — value source: a, b
 *     kmila_gate_xor     — value: two inputs → xor
 *     kmila_gate_and     — value: two inputs → and
 *     kmila_output_assign — stack block: target ← value
 *
 *   E4 (counter, clocked):
 *     kmila_on_clock      — wrapper stack ("on rising_edge(clk):") with statement input
 *     kmila_increment_count — stack block: count <= count + 1
 *
 * The companion C# service BlockProgram.cs fingerprints the user's
 * workspace and looks up a pre-baked execution timeline. This file knows
 * NOTHING about the catalog — it just defines shapes.
 */

(function () {
    if (typeof Blockly === 'undefined') {
        console.warn('[kmila-blockly] Blockly not loaded yet — concepts-blocks.js init skipped');
        return;
    }

    /* ── Kmila Blockly themes ───────────────────────────────────────────────
     * Two themes — light + dark — built off Blockly's Classic so we inherit
     * sensible defaults for anything we don't override. Block styles are
     * keyed by short names that match what the block definitions below
     * reference via `setStyle('kmila_xxx_style')`. Component styles cover
     * the workspace canvas + toolbox + flyout + scrollbars + selection.
     *
     * Light values map to Kmila's slate-brand surface tokens (slate-50 / 100
     * / 300 / 600 plus the accent #4f8cff). Dark values use slate-800 / 900
     * for backgrounds and slate-300 / 400 for foregrounds.
     *
     * The block face colours are kept (steps purple-indigo, gates sky,
     * signals emerald, accent blue, clock amber) — they're already the
     * pedagogical signal the lessons rely on. Only the canvas chrome
     * (background, toolbox, scrollbars, selection halo) changes per theme.
     * ───────────────────────────────────────────────────────────────────── */

    const BLOCK_STYLES = {
        kmila_step_style:    { colourPrimary: '#6366f1', colourSecondary: '#818cf8', colourTertiary: '#4338ca' },
        kmila_gate_style:    { colourPrimary: '#0ea5e9', colourSecondary: '#38bdf8', colourTertiary: '#0369a1' },
        kmila_signal_style:  { colourPrimary: '#10b981', colourSecondary: '#34d399', colourTertiary: '#047857' },
        kmila_clock_style:   { colourPrimary: '#f59e0b', colourSecondary: '#fbbf24', colourTertiary: '#b45309' },
        kmila_accent_style:  { colourPrimary: '#4f8cff', colourSecondary: '#93b9ff', colourTertiary: '#1e4fc0' }
    };

    const LIGHT_COMPONENT_STYLES = {
        workspaceBackgroundColour:  '#f8fafc', // slate-50
        toolboxBackgroundColour:    '#ffffff',
        toolboxForegroundColour:    '#475569', // slate-600
        flyoutBackgroundColour:     '#f1f5f9', // slate-100
        flyoutForegroundColour:     '#334155', // slate-700
        flyoutOpacity:              0.97,
        scrollbarColour:            '#cbd5e1', // slate-300
        scrollbarOpacity:           0.6,
        insertionMarkerColour:      '#4f8cff',
        insertionMarkerOpacity:     0.5,
        markerColour:               '#4f8cff',
        cursorColour:               '#4f8cff',
        selectedGlowColour:         '#4f8cff',
        selectedGlowSize:           0.6,
        replacementGlowColour:      '#94a3b8',
        replacementGlowSize:        2
    };

    const DARK_COMPONENT_STYLES = {
        workspaceBackgroundColour:  '#0f172a', // slate-900
        toolboxBackgroundColour:    '#1e293b', // slate-800
        toolboxForegroundColour:    '#cbd5e1', // slate-300
        flyoutBackgroundColour:     '#1e293b',
        flyoutForegroundColour:     '#cbd5e1',
        flyoutOpacity:              0.97,
        scrollbarColour:            '#475569', // slate-600
        scrollbarOpacity:           0.7,
        insertionMarkerColour:      '#93b9ff',
        insertionMarkerOpacity:     0.6,
        markerColour:               '#93b9ff',
        cursorColour:               '#93b9ff',
        selectedGlowColour:         '#93b9ff',
        selectedGlowSize:           0.6,
        replacementGlowColour:      '#64748b',
        replacementGlowSize:        2
    };

    const FONT_STYLE = {
        family: 'system-ui, -apple-system, "Segoe UI", Roboto, Helvetica, Arial, sans-serif',
        weight: '500',
        size:   13
    };

    // Blockly.Theme.defineTheme is available in v11; we build off Classic so
    // built-in blocks (if anyone ever drops them in) still look reasonable.
    const KmilaLightTheme = Blockly.Theme.defineTheme('kmilaLight', {
        base: Blockly.Themes.Classic,
        blockStyles: BLOCK_STYLES,
        categoryStyles: {},
        componentStyles: LIGHT_COMPONENT_STYLES,
        fontStyle: FONT_STYLE,
        startHats: false
    });

    const KmilaDarkTheme = Blockly.Theme.defineTheme('kmilaDark', {
        base: Blockly.Themes.Classic,
        blockStyles: BLOCK_STYLES,
        categoryStyles: {},
        componentStyles: DARK_COMPONENT_STYLES,
        fontStyle: FONT_STYLE,
        startHats: false
    });

    /** Returns the Kmila theme matching the current Bootstrap-theme mode. */
    function _kmilaPickTheme() {
        try {
            const mode = document.documentElement.getAttribute('data-bs-theme');
            return mode === 'dark' ? KmilaDarkTheme : KmilaLightTheme;
        } catch {
            return KmilaLightTheme;
        }
    }

    /* ── E1 / E2 ───────────────────────────────────────────────────────── */

    Blockly.Blocks['kmila_recipe_step'] = {
        init: function () {
            this.appendDummyInput()
                .appendField(new Blockly.FieldDropdown([
                    ['🍕 Knead the dough',          'knead'],
                    ['🥫 Spread the sauce',          'sauce'],
                    ['🧀 Add the cheese',            'cheese'],
                    ['🌿 Add the toppings',          'toppings'],
                    ['🔥 Bake at 220°C',             'bake'],
                    ['🍕 Slice and serve',           'serve'],
                    ['🥕 Chop the vegetables',       'chop'],
                    ['🥩 Marinate the meat',         'marinate'],
                    ['🍳 Fry the eggs',              'fry'],
                    ['🍝 Boil the pasta',            'boil']
                ]), 'STEP');
            this.setPreviousStatement(true, 'kmila_step');
            this.setNextStatement(true, 'kmila_step');
            this.setStyle('kmila_step_style');
            this.setTooltip('One step in the recipe. Drag and drop to reorder.');
        }
    };

    /* ── E3 (half-adder) ──────────────────────────────────────────────── */

    Blockly.Blocks['kmila_signal_in'] = {
        init: function () {
            this.appendDummyInput()
                .appendField(new Blockly.FieldDropdown([
                    ['a', 'a'], ['b', 'b']
                ]), 'NAME');
            this.setOutput(true, 'kmila_signal');
            this.setStyle('kmila_signal_style');
            this.setTooltip('Read an input signal value.');
        }
    };

    Blockly.Blocks['kmila_gate_xor'] = {
        init: function () {
            this.appendValueInput('LHS').setCheck('kmila_signal');
            this.appendDummyInput().appendField('XOR');
            this.appendValueInput('RHS').setCheck('kmila_signal');
            this.setInputsInline(true);
            this.setOutput(true, 'kmila_signal');
            this.setStyle('kmila_gate_style');
            this.setTooltip('Exclusive-OR gate.');
        }
    };

    Blockly.Blocks['kmila_gate_and'] = {
        init: function () {
            this.appendValueInput('LHS').setCheck('kmila_signal');
            this.appendDummyInput().appendField('AND');
            this.appendValueInput('RHS').setCheck('kmila_signal');
            this.setInputsInline(true);
            this.setOutput(true, 'kmila_signal');
            this.setStyle('kmila_gate_style');
            this.setTooltip('Logical AND gate.');
        }
    };

    Blockly.Blocks['kmila_gate_or'] = {
        init: function () {
            this.appendValueInput('LHS').setCheck('kmila_signal');
            this.appendDummyInput().appendField('OR');
            this.appendValueInput('RHS').setCheck('kmila_signal');
            this.setInputsInline(true);
            this.setOutput(true, 'kmila_signal');
            this.setStyle('kmila_gate_style');
            this.setTooltip('Logical OR gate.');
        }
    };

    Blockly.Blocks['kmila_output_assign'] = {
        init: function () {
            this.appendValueInput('VALUE')
                .setCheck('kmila_signal')
                .appendField(new Blockly.FieldDropdown([
                    ['sum  <=', 'sum'],
                    ['cout <=', 'cout']
                ]), 'TARGET');
            this.setPreviousStatement(true, 'kmila_assign');
            this.setNextStatement(true, 'kmila_assign');
            this.setStyle('kmila_accent_style');
            this.setTooltip('Drive an output signal with a value.');
        }
    };

    /* ── E4 (counter) ─────────────────────────────────────────────────── */

    Blockly.Blocks['kmila_on_clock'] = {
        init: function () {
            this.appendDummyInput().appendField('on rising_edge(clk):');
            this.appendStatementInput('BODY').setCheck('kmila_clocked');
            this.setStyle('kmila_clock_style');
            this.setTooltip('Runs every time the clock ticks.');
        }
    };

    Blockly.Blocks['kmila_increment_count'] = {
        init: function () {
            this.appendDummyInput().appendField('count <= count + 1');
            this.setPreviousStatement(true, 'kmila_clocked');
            this.setNextStatement(true, 'kmila_clocked');
            this.setStyle('kmila_accent_style');
            this.setTooltip('Increment the counter register.');
        }
    };

    /* ────────────────────────────────────────────────────────────────────
     * JS interop bridge.
     *
     * The BlockEditor.razor component calls these from C# via IJSRuntime.
     * Each function is keyed by a workspace `id` (the host div's id) so
     * multiple block editors can coexist on the same page.
     * ──────────────────────────────────────────────────────────────────── */

    const _kmilaBlocklyWorkspaces = new Map();
    const _kmilaBlocklyDotnetRefs = new Map();

    /**
     * Mount a Blockly workspace into a div by id.
     * @param {string} divId            - DOM id of the host <div>.
     * @param {object} toolboxJson      - Blockly toolbox JSON (see exercise files).
     * @param {boolean} readOnly        - When true, no toolbox/drag.
     * @param {object|null} dotnetRef   - Optional DotNetObjectReference for change callbacks.
     */
    window.kmilaBlocklyInit = function (divId, toolboxJson, readOnly, dotnetRef) {
        const div = document.getElementById(divId);
        if (!div) {
            console.warn('[kmila-blockly] init: div not found', divId);
            return false;
        }
        // Tear down a previous workspace bound to this div first; rapid
        // navigations could otherwise leak DOM and event listeners.
        const prev = _kmilaBlocklyWorkspaces.get(divId);
        if (prev) {
            try { prev.dispose(); } catch { /* already disposed */ }
            _kmilaBlocklyWorkspaces.delete(divId);
        }

        const options = {
            toolbox: toolboxJson || null,
            readOnly: !!readOnly,
            sounds: false,
            media: '_content/Kmila.Shared/lib/blockly/media/',
            trashcan: !readOnly,
            theme: _kmilaPickTheme(),
            grid: { spacing: 20, length: 3, colour: 'rgba(80,100,140,0.18)', snap: true },
            zoom:  { controls: true, wheel: false, startScale: 0.95, maxScale: 1.6, minScale: 0.5 }
        };
        const ws = Blockly.inject(divId, options);
        _kmilaBlocklyWorkspaces.set(divId, ws);

        if (dotnetRef) {
            _kmilaBlocklyDotnetRefs.set(divId, dotnetRef);
            ws.addChangeListener(function (event) {
                // We only fire when the *shape* of the program changes —
                // typing into a field is fine to ignore for fingerprinting.
                if (event.type === Blockly.Events.BLOCK_MOVE ||
                    event.type === Blockly.Events.BLOCK_CREATE ||
                    event.type === Blockly.Events.BLOCK_DELETE ||
                    event.type === Blockly.Events.BLOCK_CHANGE) {
                    try { dotnetRef.invokeMethodAsync('OnWorkspaceChanged'); }
                    catch { /* dotnet ref may have been disposed during nav */ }
                }
            });
        }

        return true;
    };

    /**
     * Serialize a workspace to a stable JSON string for fingerprinting +
     * pre-baked timeline lookup. Strips coordinates / ids so two students
     * who arrange blocks differently but build the same program get the
     * same fingerprint.
     */
    window.kmilaBlocklySerialize = function (divId) {
        const ws = _kmilaBlocklyWorkspaces.get(divId);
        if (!ws) return null;
        const state = Blockly.serialization.workspaces.save(ws);
        return JSON.stringify(state);
    };

    /**
     * Load a previously-serialized state into a workspace. Used to show
     * a "solution preview" or to seed an exercise with a partial start.
     */
    window.kmilaBlocklyLoad = function (divId, stateJson) {
        const ws = _kmilaBlocklyWorkspaces.get(divId);
        if (!ws || !stateJson) return false;
        try {
            const state = typeof stateJson === 'string' ? JSON.parse(stateJson) : stateJson;
            Blockly.serialization.workspaces.load(state, ws);
            return true;
        } catch (e) {
            console.warn('[kmila-blockly] load failed', e);
            return false;
        }
    };

    /**
     * Clear all blocks from the workspace (used by the "Reset" button).
     */
    window.kmilaBlocklyClear = function (divId) {
        const ws = _kmilaBlocklyWorkspaces.get(divId);
        if (ws) ws.clear();
    };

    /**
     * Tear down. Always called from BlockEditor.razor's IDisposable so we
     * don't leak workspaces when the user navigates away.
     */
    window.kmilaBlocklyDispose = function (divId) {
        const ws = _kmilaBlocklyWorkspaces.get(divId);
        if (ws) {
            try { ws.dispose(); } catch { /* already disposed */ }
            _kmilaBlocklyWorkspaces.delete(divId);
        }
        _kmilaBlocklyDotnetRefs.delete(divId);
    };

    /**
     * Manual resize trigger — Blazor can't observe layout changes inside the
     * DotNetObjectReference flow, so the BlockEditor calls this after dock
     * changes or window resizes to let Blockly recalculate.
     */
    window.kmilaBlocklyResize = function (divId) {
        const ws = _kmilaBlocklyWorkspaces.get(divId);
        if (ws) Blockly.svgResize(ws);
    };

    /**
     * Re-apply the Kmila theme based on the current data-bs-theme attribute.
     * Call from C# (e.g. when the user toggles dark mode in Settings) so
     * the workspace canvas, toolbox, and selection halo update without a
     * full re-inject. No-op when divId isn't mounted.
     */
    window.kmilaBlocklyApplyTheme = function (divId) {
        const ws = _kmilaBlocklyWorkspaces.get(divId);
        if (ws) {
            try { ws.setTheme(_kmilaPickTheme()); } catch { /* defensive — Blockly may be tearing down */ }
        }
    };

    /**
     * Apply the current theme to ALL mounted Kmila workspaces. Useful when
     * the user toggles theme from a global setting and we don't want each
     * BlockEditor to track the mode itself.
     */
    window.kmilaBlocklyApplyThemeAll = function () {
        const theme = _kmilaPickTheme();
        for (const ws of _kmilaBlocklyWorkspaces.values()) {
            try { ws.setTheme(theme); } catch { /* ignore */ }
        }
    };
})();
