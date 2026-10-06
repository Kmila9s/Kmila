/*
 * kmila-blocks.js — Kmila's own visual-programming engine.
 *
 * Replaces Blockly with a DOM-based block renderer driven by pages.css
 * design tokens. Public surface keeps the historical Blockly-prefixed
 * names so BlockEditor.razor's interop signature is unchanged:
 *
 *   window.kmilaBlocklyInit(divId, toolboxJson, readOnly, dotnetRef)
 *   window.kmilaBlocklySerialize(divId)        -> string
 *   window.kmilaBlocklyLoad(divId, stateJson)  -> boolean
 *   window.kmilaBlocklyClear(divId)
 *   window.kmilaBlocklyDispose(divId)
 *   window.kmilaBlocklyResize(divId)           -> no-op (CSS-driven layout)
 *   window.kmilaBlocklyApplyTheme(divId)       -> no-op (data-bs-theme drives CSS)
 *   window.kmilaBlocklyApplyThemeAll()         -> no-op
 *
 * The serialized output matches Blockly's `workspaces.save()` shape exactly:
 *   { "blocks": { "blocks": [ { type, id, x, y, fields, inputs, next } ] } }
 * so BlockProgram.cs (Kmila.Shared/Services/BlockProgram.cs) keeps producing
 * the same fingerprint catalog from the same student input.
 */

(function () {

    const SNAP_PX        = 28;     // hit-radius for "this slot will catch your block"
    const PALETTE_W      = 180;    // visual width of the flyout sidebar
    const STACK_GAP      = 0;      // notch-free design: blocks butt directly
    const UNDO_CAP       = 50;
    // 2026-08-08 mobile round 11 (Fix 7) — displacement in CSS pixels
    // that a pointerdown must travel before a drag is committed. Below
    // this threshold we treat the gesture as a tap and let the browser
    // handle any horizontal-scroll intent on the palette strip (whose
    // chips carry touch-action: pan-x). Tuned to feel instant with a
    // mouse yet forgiving on touch.
    const DRAG_THRESHOLD_PX = 6;
    // 2026-08-08 mobile round 11 (Fix 9) — double-tap-to-activate model.
    // A single pointerdown on an idle block does NOT initiate a drag —
    // the browser is free to pan the canvas. A second pointerdown on
    // the same block within DOUBLE_TAP_MS + DOUBLE_TAP_PX of the first
    // "picks it up" (adds .kmila-block--active), which flips its
    // touch-action to `none` and unlocks the drag path. Tap elsewhere
    // or press Escape to deactivate.
    const DOUBLE_TAP_MS  = 350;
    const DOUBLE_TAP_PX  = 24;

    /* ─────────────────────────────────────────────────────────────────────
     * Block-type registry
     * ──────────────────────────────────────────────────────────────────── */

    const BLOCK_DEFS = {};

    function defineBlock(type, def) {
        BLOCK_DEFS[type] = def;
    }

    // The five block "styles" — names match the kmila-block--<style> CSS modifier
    // and lean on pages.css surface tokens so dark mode comes free via
    // [data-bs-theme="dark"] selectors. No JS theme switching required.

    defineBlock('kmila_recipe_step', {
        style: 'step',
        prev:  'kmila_step',
        next:  'kmila_step',
        rows: [[
            { kind: 'dropdown', name: 'STEP', options: [
                ['🍕 Knead the dough',    'knead'],
                ['🥫 Spread the sauce',   'sauce'],
                ['🧀 Add the cheese',     'cheese'],
                ['🌿 Add the toppings',   'toppings'],
                ['🔥 Bake at 220°C',      'bake'],
                ['🍕 Slice and serve',    'serve'],
                ['🥕 Chop the vegetables','chop'],
                ['🥩 Marinate the meat',  'marinate'],
                ['🍳 Fry the eggs',       'fry'],
                ['🍝 Boil the pasta',     'boil']
            ]}
        ]]
    });

    defineBlock('kmila_signal_in', {
        style: 'signal',
        output: 'kmila_signal',
        rows: [[
            { kind: 'dropdown', name: 'NAME', options: [['a', 'a'], ['b', 'b']] }
        ]]
    });

    defineBlock('kmila_gate_xor', {
        style: 'gate',
        output: 'kmila_signal',
        inline: true,
        rows: [[
            { kind: 'value', name: 'LHS', check: 'kmila_signal' },
            { kind: 'label', text: 'XOR' },
            { kind: 'value', name: 'RHS', check: 'kmila_signal' }
        ]]
    });

    defineBlock('kmila_gate_and', {
        style: 'gate',
        output: 'kmila_signal',
        inline: true,
        rows: [[
            { kind: 'value', name: 'LHS', check: 'kmila_signal' },
            { kind: 'label', text: 'AND' },
            { kind: 'value', name: 'RHS', check: 'kmila_signal' }
        ]]
    });

    defineBlock('kmila_gate_or', {
        style: 'gate',
        output: 'kmila_signal',
        inline: true,
        rows: [[
            { kind: 'value', name: 'LHS', check: 'kmila_signal' },
            { kind: 'label', text: 'OR' },
            { kind: 'value', name: 'RHS', check: 'kmila_signal' }
        ]]
    });

    defineBlock('kmila_output_assign', {
        style: 'accent',
        prev:  'kmila_assign',
        next:  'kmila_assign',
        rows: [[
            { kind: 'dropdown', name: 'TARGET', options: [
                ['sum  <=', 'sum'],
                ['cout <=', 'cout']
            ]},
            { kind: 'value', name: 'VALUE', check: 'kmila_signal' }
        ]]
    });

    defineBlock('kmila_on_clock', {
        style: 'clock',
        rows: [[
            { kind: 'label', text: 'on rising_edge(clk):' }
        ]],
        statement: { name: 'BODY', check: 'kmila_clocked' }
    });

    defineBlock('kmila_increment_count', {
        style: 'accent',
        prev:  'kmila_clocked',
        next:  'kmila_clocked',
        rows: [[
            { kind: 'label', text: 'count <= count + 1' }
        ]]
    });

    /* ─────────────────────────────────────────────────────────────────────
     * Block instance
     * ──────────────────────────────────────────────────────────────────── */

    function genId() {
        return 'k' + Math.random().toString(36).slice(2, 11);
    }

    class Block {
        constructor(workspace, type, id) {
            const def = BLOCK_DEFS[type];
            if (!def) throw new Error('Unknown block type: ' + type);

            this.workspace = workspace;
            this.type      = type;
            this.def       = def;
            this.id        = id || genId();

            this.fields    = {};   // field name -> value
            this.inputs    = {};   // value-input name -> child Block
            this.bodyChildren = {};// statement-input name -> head Block
            this.nextBlock = null; // next-in-stack Block

            this.parent    = null; // { block, role: 'next'|'value'|'statement', name? }

            this.x = 0;
            this.y = 0;

            this.fieldEls   = {};
            this.valueSlots = {};
            this.bodySlots  = {};  // statement-input name -> slot element
            this.nextSlot   = null;

            // Seed default field values from the first dropdown option / text-field default.
            for (const row of def.rows) {
                for (const part of row) {
                    if (part.kind === 'dropdown' && part.options.length) {
                        this.fields[part.name] = part.options[0][1];
                    } else if (part.kind === 'text-field') {
                        this.fields[part.name] = part.default != null ? String(part.default) : '';
                    }
                }
            }

            this.el = this._build();
        }

        _build() {
            const def = this.def;
            const el = document.createElement('div');
            el.className = 'kmila-block kmila-block--' + def.style;
            if (def.inline)     el.classList.add('kmila-block--inline');
            if (def.output)     el.classList.add('kmila-block--has-output');
            if (def.prev)       el.classList.add('kmila-block--has-prev');
            if (def.next)       el.classList.add('kmila-block--has-next');
            if (def.statement)  el.classList.add('kmila-block--wrapper');
            el.dataset.blockId   = this.id;
            el.dataset.blockType = this.type;
            if (def.output)     el.dataset.outputCheck = def.output;
            if (def.prev)       el.dataset.prevCheck   = def.prev;

            el.setAttribute('role', 'group');
            el.setAttribute('aria-label', def.style + ' block: ' + this.type);
            el.tabIndex = 0;

            for (const row of def.rows) {
                const rowEl = document.createElement('div');
                rowEl.className = 'kmila-block__row';
                for (const part of row) rowEl.appendChild(this._renderPart(part));
                el.appendChild(rowEl);
            }

            // Statement slots — `def.statements: [{name, check, label?}]` for
            // multi-slot containers (entity/arch, if/elsif/else). Legacy
            // `def.statement: {name, check}` is wrapped into a single-element
            // array so older block defs keep working unchanged.
            const stmts = def.statements
                || (def.statement ? [def.statement] : []);
            for (const stmt of stmts) {
                if (stmt.label) {
                    const lab = document.createElement('div');
                    lab.className = 'kmila-block__statement-label';
                    lab.textContent = stmt.label;
                    el.appendChild(lab);
                }
                const slot = document.createElement('div');
                slot.className = 'kmila-block__statement kmila-slot';
                slot.dataset.statementName  = stmt.name;
                slot.dataset.statementCheck = stmt.check || '';
                el.appendChild(slot);
                this.bodySlots[stmt.name] = slot;
            }

            if (def.next) {
                this.nextSlot = document.createElement('div');
                this.nextSlot.className = 'kmila-block__next-slot kmila-slot';
                this.nextSlot.dataset.nextCheck = def.next;
                el.appendChild(this.nextSlot);
            }

            return el;
        }

        _renderPart(part) {
            if (part.kind === 'label') {
                const span = document.createElement('span');
                span.className = 'kmila-block__field kmila-block__field--label';
                span.textContent = part.text;
                return span;
            }
            if (part.kind === 'dropdown') {
                const select = document.createElement('select');
                select.className = 'kmila-block__field kmila-block__field--dropdown';
                select.dataset.fieldName = part.name;
                for (const [label, value] of part.options) {
                    const opt = document.createElement('option');
                    opt.value = value;
                    opt.textContent = label;
                    select.appendChild(opt);
                }
                select.value = this.fields[part.name];
                // Field edits are shape changes for fingerprinting — fire OnChange.
                select.addEventListener('change', () => {
                    this.fields[part.name] = select.value;
                    this.workspace._captureUndoSnapshot();
                    this.workspace._fireChange();
                });
                // Don't let the drag handler swallow native select interaction.
                select.addEventListener('pointerdown', (e) => e.stopPropagation());
                this.fieldEls[part.name] = select;
                return select;
            }
            if (part.kind === 'text-field') {
                const input = document.createElement('input');
                input.type  = 'text';
                input.className = 'kmila-block__field kmila-block__field--text';
                input.dataset.fieldName = part.name;
                if (part.placeholder) input.placeholder = part.placeholder;
                input.value = this.fields[part.name] != null
                    ? this.fields[part.name]
                    : (part.default != null ? String(part.default) : '');
                input.size = Math.max(4, (input.value || part.placeholder || '').length || 4);
                input.addEventListener('input', () => {
                    this.fields[part.name] = input.value;
                    input.size = Math.max(4, input.value.length || 1);
                });
                input.addEventListener('change', () => {
                    this.workspace._captureUndoSnapshot();
                    this.workspace._fireChange();
                });
                input.addEventListener('pointerdown', (e) => e.stopPropagation());
                this.fieldEls[part.name] = input;
                return input;
            }
            if (part.kind === 'value') {
                const slot = document.createElement('div');
                slot.className = 'kmila-block__value kmila-slot';
                slot.dataset.valueName  = part.name;
                slot.dataset.valueCheck = part.check || '';
                this.valueSlots[part.name] = slot;
                return slot;
            }
            return document.createElement('span');
        }

        setField(name, value) {
            this.fields[name] = value;
            const el = this.fieldEls[name];
            if (el) {
                el.value = value;
                if (el.tagName === 'INPUT') {
                    el.size = Math.max(4, String(value || '').length || 1);
                }
            }
        }

        // Walk this block + everything it owns (children + next chain).
        forEachDescendant(fn) {
            fn(this);
            for (const child of Object.values(this.inputs)) child.forEachDescendant(fn);
            for (const child of Object.values(this.bodyChildren)) child.forEachDescendant(fn);
            if (this.nextBlock) this.nextBlock.forEachDescendant(fn);
        }

        serialize() {
            const node = { type: this.type, id: this.id };

            if (Object.keys(this.fields).length > 0) {
                node.fields = {};
                for (const k of Object.keys(this.fields)) {
                    node.fields[k] = this.fields[k];
                }
            }

            const inputs = {};
            for (const [name, child] of Object.entries(this.inputs)) {
                inputs[name] = { block: child.serialize() };
            }
            for (const [name, child] of Object.entries(this.bodyChildren)) {
                inputs[name] = { block: child.serialize() };
            }
            if (Object.keys(inputs).length > 0) node.inputs = inputs;

            if (this.nextBlock) {
                node.next = { block: this.nextBlock.serialize() };
            }

            return node;
        }
    }

    /* ─────────────────────────────────────────────────────────────────────
     * Workspace
     * ──────────────────────────────────────────────────────────────────── */

    class Workspace {
        constructor(divId, toolboxJson, readOnly, dotnetRef) {
            this.divId    = divId;
            this.root     = document.getElementById(divId);
            this.readOnly = !!readOnly;
            this.dotnetRef = dotnetRef || null;

            this.blocks   = new Map();     // id -> Block
            this.topLevel = new Set();     // Blocks with no parent

            this._undo = [];
            this._redo = [];

            this._dragging      = null;
            this._dragOffset    = { x: 0, y: 0 };
            this._snapCandidate = null;
            this._fromPalette   = false;
            // #6 zoom state. Blocks live at model (unscaled) coords in
            // this.contentEl; a CSS transform scales it, and this.sizerEl
            // establishes the scroll range (transform doesn't affect scroll).
            this._zoom    = 1;
            this._minZoom = 0.4;
            this._maxZoom = 2;
            // 2026-08-08 mobile round 11 (Fix 9) — double-tap-to-activate state.
            this._activeBlockEl = null;
            this._lastTap       = null;

            this._build();
            this._renderPalette(toolboxJson);
            this._attachHandlers();
        }

        /* ── DOM scaffold ────────────────────────────────────────────── */

        _build() {
            this.root.innerHTML = '';
            this.root.classList.add('kmila-block-root');

            this.paletteEl = document.createElement('div');
            this.paletteEl.className = 'kmila-block-palette';
            this.paletteEl.setAttribute('aria-label', 'Block palette');

            this.canvasEl = document.createElement('div');
            this.canvasEl.className = 'kmila-block-canvas';
            this.canvasEl.setAttribute('role', 'region');
            this.canvasEl.setAttribute('aria-label', 'Block workspace');

            // #6 zoom: blocks are appended to contentEl (transform-scaled);
            // sizerEl is an invisible spacer sized to the scaled bounding box so
            // the canvas has the right scroll range at any zoom (CSS transform
            // does not contribute to scrollWidth/Height on its own).
            this.sizerEl = document.createElement('div');
            this.sizerEl.className = 'kmila-block-canvas__sizer';
            this.sizerEl.setAttribute('aria-hidden', 'true');
            this.contentEl = document.createElement('div');
            this.contentEl.className = 'kmila-block-canvas__content';
            this.contentEl.style.transformOrigin = '0 0';
            this.canvasEl.appendChild(this.sizerEl);
            this.canvasEl.appendChild(this.contentEl);

            this.trashEl = document.createElement('div');
            this.trashEl.className = 'kmila-block-trash';
            // Phase G (2026-06-28) — surface the canvas trash zone to
            // assistive tech as a discoverable drop region instead of
            // hiding it entirely. The toolbar carries the explicit
            // delete-all button; this one only activates while dragging.
            this.trashEl.setAttribute('role', 'region');
            this.trashEl.setAttribute('aria-label', 'Drag here to delete');
            this.trashEl.innerHTML =
                '<svg viewBox="0 0 16 16" width="20" height="20" fill="currentColor"><path d="M5.5 5.5A.5.5 0 0 1 6 6v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm2.5 0a.5.5 0 0 1 .5.5v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm3 .5a.5.5 0 0 0-1 0v6a.5.5 0 0 0 1 0V6z"/><path d="M14.5 3a1 1 0 0 1-1 1H13v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4h-.5a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1H6a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1h3.5a1 1 0 0 1 1 1v1zM4.118 4 4 4.059V13a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1V4.059L11.882 4H4.118zM2.5 3h11V2h-11v1z"/></svg>';
            this.canvasEl.appendChild(this.trashEl);

            this.root.appendChild(this.paletteEl);
            this.root.appendChild(this.canvasEl);

            if (this.readOnly) {
                this.paletteEl.style.display = 'none';
                this.trashEl.style.display   = 'none';
            }
        }

        // 2026-07-20 Phase 23 — categoryToolbox support.
        // 2026-08-08 mobile round 11 (Fix 2 + Fix 3) — palette gains a
        //   category-tab pill row at its top and a horizontally-
        //   scrolling chip strip below it. On phones the tab row is the
        //   only way to see the 60+ VHDL blocks without swiping through
        //   23× viewport widths; on desktop the pills stack vertically
        //   above the flow (CSS handles the mode swap). Every palette
        //   clone also gains `.kmila-block--palette-chip` so its
        //   internal statement/next-slots are hidden in the palette
        //   preview only — the block materializes normally on the
        //   canvas after drop (a fresh Block instance is created there
        //   without this class in `_onPointerDown`).
        _renderPalette(toolboxJson) {
            if (this.readOnly) return;
            this.paletteEl.innerHTML = '';
            const contents = (toolboxJson && toolboxJson.contents) || [];

            // Collect category names up-front so the pill row can render
            // in one pass before block chips get emitted below.
            const categories = [];
            for (const item of contents) {
                if (item && item.kind === 'category') {
                    categories.push(item.name || '');
                }
            }

            if (categories.length) {
                const tabRow = document.createElement('div');
                tabRow.className = 'kmila-block-palette__tabs';
                tabRow.setAttribute('role', 'tablist');
                for (const name of categories) {
                    const tab = document.createElement('button');
                    tab.type = 'button';
                    tab.className = 'kmila-block-palette__tab';
                    tab.setAttribute('role', 'tab');
                    tab.dataset.category = name;
                    tab.textContent = name;
                    tab.addEventListener('click', () => this._setActivePaletteCategory(name));
                    tabRow.appendChild(tab);
                }
                this.paletteEl.appendChild(tabRow);
            }

            // Chip strip — the horizontally-scrolling row that hosts
            // category headers (desktop) and block chips.
            const strip = document.createElement('div');
            strip.className = 'kmila-block-palette__strip';
            this.paletteEl.appendChild(strip);
            this._paletteStripEl = strip;

            for (const item of contents) {
                const catName = (item && item.kind === 'category') ? (item.name || '') : '';
                this._renderPaletteItem(item, strip, catName);
            }

            if (categories.length) {
                this._setActivePaletteCategory(categories[0]);
            }
        }

        _renderPaletteItem(item, container, currentCategory) {
            container = container || this._paletteStripEl || this.paletteEl;
            currentCategory = currentCategory || '';
            if (!item) return;
            if (item.kind === 'sep') {
                const sep = document.createElement('div');
                sep.className = 'kmila-block-palette__sep';
                container.appendChild(sep);
                return;
            }
            if (item.kind === 'category') {
                const header = document.createElement('div');
                header.className = 'kmila-toolbox-category';
                header.dataset.category = item.name || '';
                header.textContent = item.name || '';
                container.appendChild(header);
                for (const child of (item.contents || [])) {
                    // Single-depth: skip nested categories to keep the
                    // renderer flat + predictable. Nested cats fall
                    // through as unknown kinds (silently ignored).
                    if (child.kind === 'category') continue;
                    this._renderPaletteItem(child, container, item.name || '');
                }
                return;
            }
            if (item.kind === 'block' && BLOCK_DEFS[item.type]) {
                const template = new Block(this, item.type);
                template.el.classList.add('kmila-block--palette');
                template.el.classList.add('kmila-block--palette-chip');
                template.el.dataset.paletteType = item.type;
                template.el.dataset.paletteCat = currentCategory;
                template.el.dataset.paletteVisible = 'true';
                template.el.tabIndex = 0;
                container.appendChild(template.el);
            }
        }

        // Toggle which category's chips are shown in the mobile bottom-
        // sheet palette. On desktop the CSS media query above 640px
        // ignores the `data-palette-visible="false"` selector entirely,
        // so all chips remain visible in the sidebar — this JS state
        // is a no-op on wide viewports.
        _setActivePaletteCategory(name) {
            if (!this.paletteEl) return;
            this.paletteEl.dataset.mobileCat = name || '';
            const tabs = this.paletteEl.querySelectorAll('.kmila-block-palette__tab');
            for (const t of tabs) {
                const isActive = t.dataset.category === name;
                t.setAttribute('aria-selected', isActive ? 'true' : 'false');
            }
            const chips = this.paletteEl.querySelectorAll('.kmila-block--palette');
            for (const c of chips) {
                const cat = c.dataset.paletteCat || '';
                c.dataset.paletteVisible = (cat === name || !name) ? 'true' : 'false';
            }
        }

        /* ── Block lifecycle ─────────────────────────────────────────── */

        _createBlock(type, id) {
            const block = new Block(this, type, id);
            this.blocks.set(block.id, block);
            return block;
        }

        _deleteBlock(block) {
            block.forEachDescendant((b) => this.blocks.delete(b.id));
            this.topLevel.delete(block);
            if (block.el && block.el.parentElement) block.el.remove();
        }

        /* ── Connect / detach ────────────────────────────────────────── */

        _connect(child, snap) {
            const { slot, role } = snap;
            const parent = this.blocks.get(slot.closest('.kmila-block').dataset.blockId);
            if (!parent) return;

            slot.appendChild(child.el);
            child.el.style.position = '';
            child.el.style.left     = '';
            child.el.style.top      = '';

            this.topLevel.delete(child);

            if (role === 'next') {
                parent.nextBlock = child;
                child.parent = { block: parent, role: 'next' };
            } else if (role === 'value') {
                const name = slot.dataset.valueName;
                parent.inputs[name] = child;
                child.parent = { block: parent, role: 'value', name };
            } else if (role === 'statement') {
                const name = slot.dataset.statementName;
                parent.bodyChildren[name] = child;
                child.parent = { block: parent, role: 'statement', name };
            }
        }

        _detach(block) {
            if (!block.parent) return;
            const { block: parent, role, name } = block.parent;
            if (role === 'next')           parent.nextBlock = null;
            else if (role === 'value')     delete parent.inputs[name];
            else if (role === 'statement') delete parent.bodyChildren[name];
            block.parent = null;

            // Re-pin to the canvas at its current screen position.
            const rect       = block.el.getBoundingClientRect();
            const canvasRect = this.canvasEl.getBoundingClientRect();
            this.contentEl.appendChild(block.el);
            block.x = (rect.left - canvasRect.left + this.canvasEl.scrollLeft) / this._zoom;
            block.y = (rect.top  - canvasRect.top  + this.canvasEl.scrollTop)  / this._zoom;
            block.el.style.position = 'absolute';
            block.el.style.left     = block.x + 'px';
            block.el.style.top      = block.y + 'px';

            this.topLevel.add(block);
        }

        /* ── Pointer-driven drag + snap ───────────────────────────────── */

        _attachHandlers() {
            this._onDown        = this._onPointerDown.bind(this);
            this._onMove        = this._onPointerMove.bind(this);
            this._onUp          = this._onPointerUp.bind(this);
            this._onKey         = this._onKeyDown.bind(this);
            // 2026-08-08 mobile round 11 (Fix 7) — provisional handlers
            // that watch the pointer during the "is this a drag or a
            // tap?" window between pointerdown and the drag threshold.
            this._onPendingMove = this._checkDragThreshold.bind(this);
            this._onPendingEnd  = this._cancelPendingIfTap.bind(this);

            this.root.addEventListener('pointerdown', this._onDown);
            this.root.addEventListener('keydown', this._onKey);
        }

        _detachHandlers() {
            this.root.removeEventListener('pointerdown', this._onDown);
            this.root.removeEventListener('keydown', this._onKey);
            document.removeEventListener('pointermove', this._onMove);
            document.removeEventListener('pointerup', this._onUp);
            document.removeEventListener('pointercancel', this._onUp);
            document.removeEventListener('pointermove', this._onPendingMove);
            document.removeEventListener('pointerup',   this._onPendingEnd);
            document.removeEventListener('pointercancel', this._onPendingEnd);
        }

        // 2026-08-08 mobile round 11 — layered pointer semantic:
        //   Fix 9  double-tap-to-activate:  first tap on an idle block
        //     records a candidate tap and DOES NOTHING else — the
        //     browser is free to pan the canvas because the block's
        //     touch-action is `pan-x pan-y`. A second tap on the same
        //     block within DOUBLE_TAP_MS + DOUBLE_TAP_PX activates it
        //     (`.kmila-block--active`), which sets its touch-action to
        //     `none` and arms the drag path.
        //   Fix 7  deferred drag start:   an active block's next
        //     pointerdown starts the drag-threshold flow. Below
        //     DRAG_THRESHOLD_PX the gesture is a plain tap; above it,
        //     `_beginDrag` clones the palette template (or detaches
        //     the canvas block) and hands off to the real drag path.
        _onPointerDown(e) {
            if (this.readOnly) return;
            // Native interactions on form controls take precedence.
            if (e.target.closest('select, option, input, textarea, button')) return;

            const blockEl = e.target.closest('.kmila-block');
            if (!blockEl) {
                // Tap on empty canvas / palette background — deactivate
                // any currently-active block. The tap otherwise passes
                // through to the browser (canvas pan, palette scroll).
                this._deactivateBlock();
                this._lastTap = null;
                return;
            }

            const blockId = blockEl.dataset.blockId || '';

            // Case A — the tap is on an already-active block:
            // arm the drag-threshold path immediately.
            if (this._activeBlockEl === blockEl) {
                this._pending = {
                    blockEl,
                    pointerId: e.pointerId,
                    downX: e.clientX,
                    downY: e.clientY,
                };
                blockEl.classList.add('kmila-block--pressing');
                document.addEventListener('pointermove', this._onPendingMove);
                document.addEventListener('pointerup',   this._onPendingEnd);
                document.addEventListener('pointercancel', this._onPendingEnd);
                return;
            }

            // Case B — first or second tap on an inactive block.
            // Deactivate any previously-active OTHER block first.
            if (this._activeBlockEl && this._activeBlockEl !== blockEl) {
                this._deactivateBlock();
            }

            const now  = (typeof performance !== 'undefined' && performance.now) ? performance.now() : Date.now();
            const last = this._lastTap;
            const isDoubleTap = last
                && last.blockId === blockId
                && (now - last.time) < DOUBLE_TAP_MS
                && Math.hypot(e.clientX - last.x, e.clientY - last.y) < DOUBLE_TAP_PX;

            if (isDoubleTap) {
                this._activateBlock(blockEl);
                this._lastTap = null;
                // Do NOT arm the drag path yet — the "activate" gesture
                // is its own act. The user's next pointerdown on the
                // now-active block starts the drag.
                return;
            }

            // First-tap-of-a-pair. Just remember it. Do not preventDefault:
            // the browser handles pan/scroll for this touch as usual.
            this._lastTap = { blockId, time: now, x: e.clientX, y: e.clientY };
        }

        // 2026-08-08 mobile round 11 (Fix 9) — visual + behavioural
        // "picked up" state. Sets .kmila-block--active on the target,
        // clears it on any prior active block, and remembers the ref
        // for pointerdown case A above.
        _activateBlock(blockEl) {
            if (this._activeBlockEl === blockEl) return;
            this._deactivateBlock();
            this._activeBlockEl = blockEl;
            blockEl.classList.add('kmila-block--active');
            // Focus for keyboard accessibility (Escape deactivates).
            if (typeof blockEl.focus === 'function') {
                try { blockEl.focus({ preventScroll: true }); } catch (_) { blockEl.focus(); }
            }
        }

        _deactivateBlock() {
            const el = this._activeBlockEl;
            if (!el) return;
            el.classList.remove('kmila-block--active');
            el.classList.remove('kmila-block--pressing');
            this._activeBlockEl = null;
        }

        _checkDragThreshold(e) {
            const p = this._pending;
            if (!p) return;
            if (e.pointerId !== p.pointerId) return;
            const dx = e.clientX - p.downX;
            const dy = e.clientY - p.downY;
            if (Math.hypot(dx, dy) < DRAG_THRESHOLD_PX) return;

            // Threshold crossed → real drag now. Drop the pending
            // listeners and hand off to the drag path.
            document.removeEventListener('pointermove', this._onPendingMove);
            document.removeEventListener('pointerup',   this._onPendingEnd);
            document.removeEventListener('pointercancel', this._onPendingEnd);

            const blockEl = p.blockEl;
            this._pending = null;
            e.preventDefault();
            this._beginDrag(e, blockEl);
        }

        _cancelPendingIfTap() {
            const p = this._pending;
            if (!p) return;
            if (p.blockEl) p.blockEl.classList.remove('kmila-block--pressing');
            this._pending = null;
            document.removeEventListener('pointermove', this._onPendingMove);
            document.removeEventListener('pointerup',   this._onPendingEnd);
            document.removeEventListener('pointercancel', this._onPendingEnd);
        }

        _beginDrag(e, blockEl) {
            blockEl.classList.remove('kmila-block--pressing');

            let block;
            const paletteType = blockEl.dataset.paletteType;
            if (paletteType) {
                // Cloning a palette template — never modify the palette itself.
                block = this._createBlock(paletteType);
                this.topLevel.add(block);
                this.contentEl.appendChild(block.el);
                const canvasRect  = this.canvasEl.getBoundingClientRect();
                const templateRect = blockEl.getBoundingClientRect();
                block.x = (templateRect.left - canvasRect.left + this.canvasEl.scrollLeft) / this._zoom;
                block.y = (templateRect.top  - canvasRect.top  + this.canvasEl.scrollTop)  / this._zoom;
                block.el.style.position = 'absolute';
                block.el.style.left     = block.x + 'px';
                block.el.style.top      = block.y + 'px';
                this._fromPalette = true;
                this._dragOffset.x = e.clientX - templateRect.left;
                this._dragOffset.y = e.clientY - templateRect.top;
            } else {
                block = this.blocks.get(blockEl.dataset.blockId);
                if (!block) return;
                this._captureUndoSnapshot();
                const blockRect = block.el.getBoundingClientRect();
                this._dragOffset.x = e.clientX - blockRect.left;
                this._dragOffset.y = e.clientY - blockRect.top;
                this._detach(block);
                this._fromPalette = false;
            }

            this._dragging = block;
            block.el.classList.add('kmila-block--dragging');
            // Pulled forward to land above peers + the trash icon.
            block.el.style.zIndex = '1000';

            document.addEventListener('pointermove', this._onMove);
            document.addEventListener('pointerup',   this._onUp);
            document.addEventListener('pointercancel', this._onUp);

            // Prime the block position with the current pointer so it
            // doesn't jump on the first genuine pointermove — the drag
            // threshold consumed the pointer events between down and now.
            this._onPointerMove(e);
        }

        _onPointerMove(e) {
            if (!this._dragging) return;
            const block = this._dragging;
            const canvasRect = this.canvasEl.getBoundingClientRect();
            block.x = (e.clientX - canvasRect.left - this._dragOffset.x + this.canvasEl.scrollLeft) / this._zoom;
            block.y = (e.clientY - canvasRect.top  - this._dragOffset.y + this.canvasEl.scrollTop)  / this._zoom;
            block.el.style.left = block.x + 'px';
            block.el.style.top  = block.y + 'px';

            this._recomputeBounds();
            this._updateSnapCandidate();
            this._updateTrashHover(e.clientX, e.clientY);
        }

        _onPointerUp(e) {
            if (!this._dragging) return;
            const block = this._dragging;
            const snap  = this._snapCandidate;

            document.removeEventListener('pointermove', this._onMove);
            document.removeEventListener('pointerup',   this._onUp);
            document.removeEventListener('pointercancel', this._onUp);

            block.el.classList.remove('kmila-block--dragging');
            block.el.style.zIndex = '';
            this._clearSnapPreview();
            this._dragging      = null;
            this._snapCandidate = null;
            this.trashEl.classList.remove('kmila-block-trash--active');

            if (this._isOverTrash(e.clientX, e.clientY) && !this._fromPalette) {
                // Palette clones thrown back at the palette just vanish silently.
                this._deleteBlock(block);
            } else if (this._isOverPalette(e.clientX, e.clientY)) {
                // Dragging back over the palette = delete (matches Blockly UX).
                this._deleteBlock(block);
            } else if (snap) {
                this._connect(block, snap);
            } else if (this._fromPalette) {
                // First-time palette clone — captured AFTER placement so the
                // initial drop is also undoable.
                this._captureUndoSnapshot();
            }

            // 2026-08-08 mobile round 11 (Fix 9) — drop ends the
            // "picked up" state. If the student wants to move the
            // block again, they double-tap it again. Prevents drag
            // gestures from silently keeping a block armed after
            // release.
            this._deactivateBlock();
            this._lastTap = null;

            this._fromPalette = false;
            this._fireChange();
        }

        /* ── #6 Zoom ──────────────────────────────────────────────────────
           Blocks keep unscaled (model) coords; contentEl is transform-scaled
           and sizerEl holds the scroll range (= scaled bounding box). */
        _recomputeBounds() {
            if (!this.contentEl || !this.sizerEl) return;
            let maxR = 0, maxB = 0;
            for (const el of this.contentEl.children) {
                const x = parseFloat(el.style.left) || 0;
                const y = parseFloat(el.style.top)  || 0;
                maxR = Math.max(maxR, x + el.offsetWidth);
                maxB = Math.max(maxB, y + el.offsetHeight);
            }
            const z = this._zoom;
            // +40 model px slack so the last block never sits flush to the edge.
            this.sizerEl.style.width  = Math.ceil((maxR + 40) * z) + 'px';
            this.sizerEl.style.height = Math.ceil((maxB + 40) * z) + 'px';
        }

        _applyZoom() {
            if (!this.contentEl) return;
            this.contentEl.style.transform = 'scale(' + this._zoom + ')';
            this._recomputeBounds();
        }

        zoomBy(factor) {
            const z = Math.min(this._maxZoom, Math.max(this._minZoom, this._zoom * factor));
            this._zoom = Math.round(z * 100) / 100;
            this._applyZoom();
            return this._zoom;
        }

        zoomReset() {
            this._zoom = 1;
            this._applyZoom();
            return 1;
        }

        /* ── #6 Phase C: export as PNG / SVG / PDF ────────────────────────
           The canvas is HTML, so we serialise the content layer into an SVG
           <foreignObject> (styles inlined, inputs flattened to text, the mono
           font embedded), always at 1× regardless of the current zoom. SVG is
           downloaded directly; PNG rasterises the SVG via an <img>→<canvas>;
           PDF wraps that PNG with the bundled jsPDF. */
        _exportModelBounds() {
            let w = 0, h = 0;
            for (const el of this.contentEl.children) {
                w = Math.max(w, (parseFloat(el.style.left) || 0) + el.offsetWidth);
                h = Math.max(h, (parseFloat(el.style.top)  || 0) + el.offsetHeight);
            }
            return { w: Math.ceil(w), h: Math.ceil(h) };
        }

        async exportAs(format) {
            const b = this._exportModelBounds();
            if (!(b.w > 0) || !(b.h > 0)) return false;
            try { return await kmilaBlocksRunExport(this, b, format); }
            catch (_) { return false; }
        }

        _updateSnapCandidate() {
            this._clearSnapPreview();
            this._snapCandidate = null;

            const block = this._dragging;
            const def   = block.def;

            // What outgoing connector are we trying to land?
            //   has prev    -> any compatible empty `next` or `statement` slot
            //   has output  -> any compatible empty `value` slot
            // Wrapper blocks (no prev/output) can only sit at top level — no snap.

            const candidates = [];
            const blockRect  = block.el.getBoundingClientRect();
            let px, py;

            if (def.prev) {
                px = blockRect.left + 12;
                py = blockRect.top;
                this._collectStackCandidates(block, def.prev, candidates);
            }
            if (def.output) {
                px = blockRect.left;
                py = blockRect.top + blockRect.height / 2;
                this._collectValueCandidates(block, def.output, candidates);
            }

            let best = null, bestDist = Infinity;
            for (const cand of candidates) {
                const r = cand.slot.getBoundingClientRect();
                let cx, cy;
                if (cand.role === 'value') {
                    cx = r.left + 6;
                    cy = r.top + r.height / 2;
                } else {
                    cx = r.left + 12;
                    cy = r.top;
                }
                const dx = cx - px, dy = cy - py;
                const d  = Math.hypot(dx, dy);
                if (d < SNAP_PX && d < bestDist) {
                    bestDist = d;
                    best     = cand;
                }
            }

            if (best) {
                this._snapCandidate = best;
                best.slot.classList.add('kmila-slot--snap');
            }
        }

        _collectStackCandidates(dragging, type, out) {
            // Every empty next-slot + every empty statement-slot of compatible type
            // that isn't inside the dragging subtree.
            const nextSlots = this.canvasEl.querySelectorAll(
                '.kmila-block__next-slot[data-next-check="' + type + '"]'
            );
            for (const slot of nextSlots) {
                if (slot.firstElementChild) continue;
                if (dragging.el.contains(slot)) continue;
                out.push({ slot, role: 'next' });
            }
            const stmtSlots = this.canvasEl.querySelectorAll(
                '.kmila-block__statement[data-statement-check="' + type + '"]'
            );
            for (const slot of stmtSlots) {
                if (slot.firstElementChild) continue;
                if (dragging.el.contains(slot)) continue;
                out.push({ slot, role: 'statement' });
            }
        }

        _collectValueCandidates(dragging, type, out) {
            const slots = this.canvasEl.querySelectorAll(
                '.kmila-block__value[data-value-check="' + type + '"]'
            );
            for (const slot of slots) {
                if (slot.firstElementChild) continue;
                if (dragging.el.contains(slot)) continue;
                out.push({ slot, role: 'value' });
            }
        }

        _clearSnapPreview() {
            for (const el of this.canvasEl.querySelectorAll('.kmila-slot--snap')) {
                el.classList.remove('kmila-slot--snap');
            }
        }

        /* ── Trash / palette hit-test ────────────────────────────────── */

        _updateTrashHover(clientX, clientY) {
            if (this._isOverTrash(clientX, clientY) || this._isOverPalette(clientX, clientY)) {
                this.trashEl.classList.add('kmila-block-trash--active');
                this._dragging.el.classList.add('kmila-block--over-trash');
            } else {
                this.trashEl.classList.remove('kmila-block-trash--active');
                this._dragging.el.classList.remove('kmila-block--over-trash');
            }
        }

        _isOverTrash(clientX, clientY) {
            const r = this.trashEl.getBoundingClientRect();
            return clientX >= r.left && clientX <= r.right && clientY >= r.top && clientY <= r.bottom;
        }

        _isOverPalette(clientX, clientY) {
            if (this.readOnly) return false;
            const r = this.paletteEl.getBoundingClientRect();
            return clientX >= r.left && clientX <= r.right && clientY >= r.top && clientY <= r.bottom;
        }

        /* ── Keyboard: undo / redo ───────────────────────────────────── */

        _onKeyDown(e) {
            // Escape deactivates the current active block — the tap-out
            // gesture for keyboard users. Fires even without a modifier.
            if (e.key === 'Escape' && this._activeBlockEl) {
                e.preventDefault();
                this._deactivateBlock();
                return;
            }
            const mod = e.ctrlKey || e.metaKey;
            if (!mod) return;
            const k = e.key.toLowerCase();
            if (k === 'z' && !e.shiftKey) {
                e.preventDefault();
                this._undoOnce();
            } else if ((k === 'z' && e.shiftKey) || k === 'y') {
                e.preventDefault();
                this._redoOnce();
            }
        }

        /* ── Undo / redo (state-snapshot strategy) ───────────────────── */

        _captureUndoSnapshot() {
            const snap = this._serializeRaw();
            this._undo.push(snap);
            if (this._undo.length > UNDO_CAP) this._undo.shift();
            this._redo = [];
        }

        _undoOnce() {
            if (this._undo.length === 0) return;
            const current = this._serializeRaw();
            const prev    = this._undo.pop();
            this._redo.push(current);
            this._loadRaw(prev);
            this._fireChange();
        }

        _redoOnce() {
            if (this._redo.length === 0) return;
            const current = this._serializeRaw();
            const next    = this._redo.pop();
            this._undo.push(current);
            this._loadRaw(next);
            this._fireChange();
        }

        /* ── Public serialize / load / clear ──────────────────────────── */

        serialize() {
            return JSON.stringify(this._serializeRaw());
        }

        _serializeRaw() {
            const top = [];
            for (const block of this.topLevel) {
                const node = block.serialize();
                // Keep coordinates on top-level blocks for round-trip stability.
                // BlockProgram.cs ignores x/y; they're for our load() to put
                // each stack back where the student left it.
                node.x = block.x;
                node.y = block.y;
                top.push(node);
            }
            return { blocks: { blocks: top } };
        }

        load(stateJsonOrObj) {
            try {
                const state = typeof stateJsonOrObj === 'string'
                    ? JSON.parse(stateJsonOrObj)
                    : stateJsonOrObj;
                this._loadRaw(state);
                return true;
            } catch (e) {
                console.warn('[kmila-blocks] load failed', e);
                return false;
            }
        }

        _loadRaw(state) {
            this._clearInternal();
            const top = state && state.blocks && state.blocks.blocks;
            if (!Array.isArray(top)) return;
            const placed = [];
            for (const node of top) {
                const block = this._materialize(node);
                if (!block) continue;
                this.topLevel.add(block);
                this.contentEl.appendChild(block.el);
                block.x = typeof node.x === 'number' ? node.x : 20;
                block.y = typeof node.y === 'number' ? node.y : 20;
                block.el.style.position = 'absolute';
                block.el.style.left     = block.x + 'px';
                block.el.style.top      = block.y + 'px';
                placed.push(block);
            }
            // The auto-builder (VhdlBlockBuilder) stacks top-level blocks
            // with a fixed-height assumption that breaks when entity+arch
            // grow tall (many ports, deep process bodies). Reflow on the
            // next frame using measured heights — but only when blocks
            // look auto-generated (single x column) so we don't disturb
            // positions a user authored by dragging.
            if (placed.length > 1 && placed.every(b => b.x === placed[0].x)) {
                requestAnimationFrame(() => this._reflowStack(placed));
            }
            this._recomputeBounds();
        }

        /* Re-measure and restack the auto-generated top-level column. Only
         * touches single-column auto layouts (same guard as _loadRaw) so a
         * user's hand-dragged positions are never disturbed. Deferred to the
         * next animation frame so offsetHeight reads the real, post-paint size
         * (the canvas may have been display:none until a moment ago). */
        reflow() {
            requestAnimationFrame(() => {
                const tops = Array.from(this.topLevel);
                if (tops.length > 1 && tops.every(b => b.x === tops[0].x)) {
                    this._reflowStack(tops);
                }
            });
        }

        _reflowStack(blocks) {
            const sorted = blocks.slice().sort((a, b) => a.y - b.y);
            const gap = 24;
            let y = sorted[0].y;
            for (const b of sorted) {
                if (!b.el.isConnected) continue;
                b.y = y;
                b.el.style.top = y + 'px';
                const h = b.el.offsetHeight || 0;
                y += h + gap;
            }
            this._recomputeBounds();
        }

        _materialize(node) {
            if (!node || !node.type || !BLOCK_DEFS[node.type]) return null;
            const block = this._createBlock(node.type, node.id);

            if (node.fields) {
                for (const [name, value] of Object.entries(node.fields)) {
                    block.setField(name, value);
                }
            }

            if (node.inputs) {
                for (const [name, slotData] of Object.entries(node.inputs)) {
                    if (!slotData || !slotData.block) continue;
                    const child = this._materialize(slotData.block);
                    if (!child) continue;
                    if (block.valueSlots[name]) {
                        block.valueSlots[name].appendChild(child.el);
                        block.inputs[name] = child;
                        child.parent = { block, role: 'value', name };
                    } else if (block.bodySlots[name]) {
                        block.bodySlots[name].appendChild(child.el);
                        block.bodyChildren[name] = child;
                        child.parent = { block, role: 'statement', name };
                    }
                }
            }

            if (node.next && node.next.block && block.nextSlot) {
                const child = this._materialize(node.next.block);
                if (child) {
                    block.nextSlot.appendChild(child.el);
                    block.nextBlock = child;
                    child.parent = { block, role: 'next' };
                }
            }

            return block;
        }

        clear() {
            this._captureUndoSnapshot();
            this._clearInternal();
            this._fireChange();
        }

        _clearInternal() {
            this.blocks.clear();
            this.topLevel.clear();
            const live = this.canvasEl.querySelectorAll('.kmila-block:not(.kmila-block--palette)');
            for (const el of live) el.remove();
        }

        dispose() {
            this._detachHandlers();
            this.root.innerHTML = '';
            this.dotnetRef = null;
        }

        /* ── DotNet callback bridge ───────────────────────────────────── */

        _fireChange() {
            if (!this.dotnetRef) return;
            try { this.dotnetRef.invokeMethodAsync('OnWorkspaceChanged'); }
            catch { /* dotnet ref may be disposed mid-navigation */ }
        }
    }

    /* ─────────────────────────────────────────────────────────────────────
     * Workspace registry + Blockly-compatible window API
     * ──────────────────────────────────────────────────────────────────── */

    const workspaces = new Map();

    window.kmilaBlocklyInit = function (divId, toolboxJson, readOnly, dotnetRef) {
        const div = document.getElementById(divId);
        if (!div) {
            console.warn('[kmila-blocks] init: div not found', divId);
            return false;
        }
        const prev = workspaces.get(divId);
        if (prev) { try { prev.dispose(); } catch { /* already gone */ } }

        const ws = new Workspace(divId, toolboxJson, readOnly, dotnetRef);
        workspaces.set(divId, ws);
        return true;
    };

    window.kmilaBlocklySerialize = function (divId) {
        const ws = workspaces.get(divId);
        return ws ? ws.serialize() : null;
    };

    window.kmilaBlocklyLoad = function (divId, stateJson) {
        const ws = workspaces.get(divId);
        if (!ws || !stateJson) return false;
        return ws.load(stateJson);
    };

    window.kmilaBlocklyClear = function (divId) {
        const ws = workspaces.get(divId);
        if (ws) ws.clear();
    };

    window.kmilaBlocklyDispose = function (divId) {
        const ws = workspaces.get(divId);
        if (ws) {
            try { ws.dispose(); } catch { /* idempotent */ }
            workspaces.delete(divId);
        }
    };

    // Re-measure + restack the auto-generated top-level column. The BlockEditor
    // lives in an always-mounted editor tab that is `display:none` at init, so
    // blocks were first measured at zero height and stacked on top of each
    // other; entering the tab calls this so they lay out against their now-real
    // heights. (Previously a no-op, which is why the Blocks view needed a second
    // tab flip before it rendered correctly — issue #5b.)
    window.kmilaBlocklyResize        = function (divId) {
        const ws = workspaces.get(divId);
        if (ws) ws.reflow();
    };
    window.kmilaBlocklyApplyTheme    = function () { };
    window.kmilaBlocklyApplyThemeAll = function () { };

    // External block families (kmila-blocks-vhdl.js, etc.) register their
    // own block types through this hook so they can live in their own file
    // without re-opening this IIFE. Pass the same shape `defineBlock`
    // accepts internally: { style, prev?, next?, output?, inline?, rows,
    // statement? | statements? }.
    window.kmilaBlocksDefine = defineBlock;

    /* ── Decoration API (Step 7 of backlog #16) ─────────────────────────
     * Toggle a CSS class on blocks by id. The Editor's diagnostic path
     * uses this to mark blocks the simulator complained about: the C#
     * side keeps a {line → blockId} map produced by the emitter, looks
     * up the ids matching each error line, and asks us to add the
     * "kmila-block--error" class to them.
     *
     *   kmilaBlocksHighlight(divId, blockIds[], cssClass)
     *   kmilaBlocksClearHighlight(divId, cssClass)                       */
    window.kmilaBlocksHighlight = function (divId, blockIds, cssClass) {
        const ws = workspaces.get(divId);
        if (!ws || !Array.isArray(blockIds) || !cssClass) return;
        const set = new Set(blockIds);
        for (const block of ws.blocks.values()) {
            if (!block.el) continue;
            if (set.has(block.id)) block.el.classList.add(cssClass);
            else                   block.el.classList.remove(cssClass);
        }
    };

    window.kmilaBlocksClearHighlight = function (divId, cssClass) {
        const ws = workspaces.get(divId);
        if (!ws || !cssClass) return;
        for (const block of ws.blocks.values()) {
            if (block.el) block.el.classList.remove(cssClass);
        }
    };

    /* ── Phase G (2026-06-28) — toolbar bindings ────────────────────────
     * Floating toolbar inside the BlockEditor.razor card calls these from
     * its button @onclick handlers. Keep names short — they're invoked as
     * `JS.InvokeVoidAsync` arguments from C#. */
    window.kmilaBlocksUndo = function (divId) {
        const ws = workspaces.get(divId);
        if (ws) ws._undoOnce();
    };
    window.kmilaBlocksRedo = function (divId) {
        const ws = workspaces.get(divId);
        if (ws) ws._redoOnce();
    };
    // Fit-to-screen: reset zoom to 1× and scroll the canvas back to its origin
    // so blocks that drifted off-viewport (or shrank under zoom) become
    // reachable again.
    window.kmilaBlocksFit = function (divId) {
        const ws = workspaces.get(divId);
        if (!ws) return;
        if (ws.zoomReset) ws.zoomReset();
        if (ws.canvasEl)
            ws.canvasEl.scrollTo({ left: 0, top: 0, behavior: 'smooth' });
    };
    // #6 Zoom in/out. `factor` > 1 zooms in, < 1 zooms out. Returns the new
    // zoom level (clamped) so the toolbar can show a %-readout.
    window.kmilaBlocksZoom = function (divId, factor) {
        const ws = workspaces.get(divId);
        return ws && ws.zoomBy ? ws.zoomBy(factor) : 1;
    };
    window.kmilaBlocksZoomReset = function (divId) {
        const ws = workspaces.get(divId);
        return ws && ws.zoomReset ? ws.zoomReset() : 1;
    };

    /* ── #6 Phase C export helpers (module scope) ─────────────────────── */
    // Curated computed-style props to inline onto the export clone (same set
    // the visual-canvas exporter uses, plus the left-border role tint + text
    // ellipsis bits the blocks rely on).
    const BLOCK_EXPORT_PROPS = [
        'position', 'left', 'top', 'width', 'height', 'min-width', 'min-height',
        'box-sizing', 'display', 'flex-direction', 'flex-wrap', 'align-items',
        'justify-content', 'gap', 'color', 'background', 'background-color',
        'background-image', 'border', 'border-color', 'border-width',
        'border-style', 'border-radius', 'border-left', 'border-inline-start',
        'box-shadow', 'padding', 'margin', 'font-family', 'font-size',
        'font-weight', 'font-variant-numeric', 'line-height', 'letter-spacing',
        'text-align', 'text-transform', 'white-space', 'text-overflow',
        'opacity', 'z-index'
    ];
    function kmilaBlocksInlineStyles(src, dst) {
        const cs = window.getComputedStyle(src);
        let s = '';
        for (const p of BLOCK_EXPORT_PROPS) {
            const v = cs.getPropertyValue(p);
            if (v && v !== 'none' && v !== 'auto' && v !== 'normal') s += p + ':' + v + ';';
        }
        dst.setAttribute('style', s);
        for (let i = 0; i < src.children.length; i++) {
            if (dst.children[i]) kmilaBlocksInlineStyles(src.children[i], dst.children[i]);
        }
    }
    // Replace every <input>/<select>/<textarea> in the clone with a styled
    // <span> showing its value — form controls don't rasterise inside an SVG
    // foreignObject, so without this they'd export blank.
    function kmilaBlocksFlattenInputs(srcRoot, cloneRoot) {
        const ss = srcRoot.querySelectorAll('input,select,textarea');
        const ds = cloneRoot.querySelectorAll('input,select,textarea');
        for (let i = 0; i < ds.length; i++) {
            const s = ss[i], d = ds[i];
            if (!s || !d) continue;
            let val = '';
            if (s.tagName === 'SELECT') val = (s.options[s.selectedIndex] && s.options[s.selectedIndex].text) || s.value || '';
            else val = s.value || s.getAttribute('placeholder') || '';
            const span = document.createElement('span');
            span.textContent = val;
            span.setAttribute('style', (d.getAttribute('style') || '') +
                ';display:inline-flex;align-items:center;white-space:nowrap;');
            d.replaceWith(span);
        }
    }
    let _blockFontCache = null;
    async function kmilaBlocksFontCss() {
        if (_blockFontCache !== null) return _blockFontCache;
        try {
            const base = '_content/Kmila.Shared/lib/jetbrains-mono/';
            const css = await fetch(base + 'jetbrains-mono.css').then(r => r.text());
            const m = css.match(/url\((fonts\/[^)]+\.woff2)\)/);
            if (!m) throw 0;
            const buf = await fetch(base + m[1]).then(r => r.arrayBuffer());
            let bin = ''; const bytes = new Uint8Array(buf);
            for (let i = 0; i < bytes.length; i++) bin += String.fromCharCode(bytes[i]);
            _blockFontCache = "@font-face{font-family:'JetBrains Mono';src:url(data:font/woff2;base64," +
                btoa(bin) + ") format('woff2');font-weight:400;font-style:normal;}";
        } catch (_) { _blockFontCache = ''; }  // best-effort; fall back to a system mono
        return _blockFontCache;
    }
    function kmilaBlocksDownload(blob, filename) {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = filename;
        document.body.appendChild(a); a.click(); a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 500);
    }
    // Rasterise the SVG to a <canvas>. Renders at 2× for crispness but caps the
    // longest side (very deep designs can be 20 000+ px tall) so PNG/PDF stay a
    // sane size; SVG export keeps full vector fidelity regardless.
    function kmilaBlocksRasterize(svg, W, H) {
        return new Promise((resolve) => {
            const img = new Image();
            img.onload = () => {
                try {
                    const MAX = 8000;
                    let scale = 2;
                    const longest = Math.max(W, H);
                    if (longest * scale > MAX) scale = MAX / longest;
                    const c = document.createElement('canvas');
                    c.width = Math.max(1, Math.round(W * scale));
                    c.height = Math.max(1, Math.round(H * scale));
                    const ctx = c.getContext('2d');
                    ctx.scale(scale, scale);
                    ctx.drawImage(img, 0, 0);
                    resolve(c);
                } catch (_) { resolve(null); }
            };
            img.onerror = () => resolve(null);
            img.src = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svg);
        });
    }
    async function kmilaBlocksRunExport(ws, bounds, format) {
        const PAD = 16;
        const W = bounds.w + PAD * 2, H = bounds.h + PAD * 2;
        const src = ws.contentEl;
        const clone = src.cloneNode(true);
        kmilaBlocksInlineStyles(src, clone);
        clone.style.transform = 'none';   // export at 1×
        clone.style.position = 'absolute';
        clone.style.left = PAD + 'px';
        clone.style.top = PAD + 'px';
        kmilaBlocksFlattenInputs(src, clone);
        const bg = window.getComputedStyle(ws.canvasEl).backgroundColor || '#0b1220';
        const wrap = document.createElement('div');
        wrap.setAttribute('xmlns', 'http://www.w3.org/1999/xhtml');
        wrap.style.cssText = 'position:relative;width:' + W + 'px;height:' + H +
            'px;background:' + bg + ';';
        wrap.appendChild(clone);
        const font = await kmilaBlocksFontCss();
        const xml = new XMLSerializer().serializeToString(wrap);
        const svg = '<?xml version="1.0" encoding="UTF-8"?>\n' +
            '<svg xmlns="http://www.w3.org/2000/svg" width="' + W + '" height="' + H +
            '" viewBox="0 0 ' + W + ' ' + H + '"><defs><style>' + font + '</style></defs>' +
            '<foreignObject x="0" y="0" width="' + W + '" height="' + H + '">' + xml +
            '</foreignObject></svg>';
        if (format === 'svg') {
            kmilaBlocksDownload(new Blob([svg], { type: 'image/svg+xml;charset=utf-8' }), 'kmila-blocks.svg');
            return true;
        }
        const canvas = await kmilaBlocksRasterize(svg, W, H);
        if (!canvas) return false;
        if (format === 'png') {
            const blob = await new Promise((res) => canvas.toBlob(res, 'image/png'));
            if (!blob) return false;
            kmilaBlocksDownload(blob, 'kmila-blocks.png');
            return true;
        }
        if (format === 'pdf') {
            if (!window.jspdf || !window.jspdf.jsPDF) return false;
            // JPEG (not PNG) in the PDF — a PNG of a 8000px-tall diagram inflates
            // jsPDF to hundreds of MB; JPEG at high quality keeps it a few MB.
            const jpeg = canvas.toDataURL('image/jpeg', 0.92);
            const pw = canvas.width, ph = canvas.height;
            const { jsPDF } = window.jspdf;
            const doc = new jsPDF({
                orientation: pw > ph ? 'landscape' : 'portrait',
                unit: 'px', format: [pw, ph], hotfixes: ['px_scaling'],
            });
            doc.addImage(jpeg, 'JPEG', 0, 0, pw, ph);
            doc.save('kmila-blocks.pdf');
            return true;
        }
        return false;
    }
    window.kmilaBlocksExport = function (divId, format) {
        const ws = workspaces.get(divId);
        return ws && ws.exportAs ? ws.exportAs(format) : Promise.resolve(false);
    };

    // Query whether undo / redo stacks are non-empty — toolbar buttons
    // grey themselves out via this when there's nothing to step.
    window.kmilaBlocksUndoState = function (divId) {
        const ws = workspaces.get(divId);
        if (!ws) return { undo: false, redo: false };
        return {
            undo: (ws._undo && ws._undo.length > 0),
            redo: (ws._redo && ws._redo.length > 0)
        };
    };
})();
