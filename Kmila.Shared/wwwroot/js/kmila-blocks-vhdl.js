/*
 * kmila-blocks-vhdl.js — VHDL author block family for the kmila-blocks engine.
 *
 * Registers Phase 0 vertical-slice block types via `window.kmilaBlocksDefine`
 * (exposed by kmila-blocks.js). Anything declared here is purely a data
 * structure in the engine's registry; the file is harmless to load on
 * every page — block defs only take effect when a workspace asks for them.
 *
 *   Slot type taxonomy (used as prev/next/output/check strings):
 *     kmila_vhdl_port       — port declarations under entity.PORTS
 *     kmila_vhdl_decl       — signal/constant declarations under arch.DECLS
 *     kmila_vhdl_concurrent — concurrent statements under arch.BODY (process, …)
 *     kmila_vhdl_seq        — sequential statements inside process/if bodies
 *     kmila_vhdl_expr       — expression atoms (id, literal, eq, rising_edge, …)
 *
 * Forward emission (workspace JSON → VHDL string) lives in
 *   Kmila.Shared/Services/VhdlBlockEmitter.cs
 * which walks the same serialization shape kmilaBlocklySerialize() produces.
 */

(function () {
    if (typeof window.kmilaBlocksDefine !== 'function') {
        console.warn('[kmila-blocks-vhdl] kmila-blocks.js must load first');
        return;
    }
    const def = window.kmilaBlocksDefine;

    /* ── Top-level: entity + architecture ──────────────────────────── */

    def('kmila_vhdl_entity', {
        style: 'clock',
        rows: [[
            { kind: 'label', text: 'entity' },
            { kind: 'text-field', name: 'NAME', placeholder: 'name', default: 'counter' },
            { kind: 'label', text: 'is' }
        ]],
        statements: [
            { name: 'PORTS', check: 'kmila_vhdl_port', label: 'port' }
        ]
    });

    def('kmila_vhdl_architecture', {
        style: 'clock',
        rows: [[
            { kind: 'label', text: 'architecture' },
            { kind: 'text-field', name: 'NAME', default: 'rtl' },
            { kind: 'label', text: 'of' },
            { kind: 'text-field', name: 'OF', default: 'counter' },
            { kind: 'label', text: 'is' }
        ]],
        statements: [
            { name: 'DECLS', check: 'kmila_vhdl_decl', label: 'declarations' },
            { name: 'BODY',  check: 'kmila_vhdl_concurrent', label: 'begin' }
        ]
    });

    /* ── Declarations: port + signal ───────────────────────────────── */

    def('kmila_vhdl_port', {
        style: 'clock',
        prev: 'kmila_vhdl_port',
        next: 'kmila_vhdl_port',
        rows: [[
            { kind: 'label', text: 'port' },
            { kind: 'text-field', name: 'NAME', default: 'clk' },
            { kind: 'label', text: ':' },
            { kind: 'dropdown', name: 'DIR', options: [
                ['in',    'in'],
                ['out',   'out'],
                ['inout', 'inout']
            ]},
            // TYPE is a free text field (was a single-option dropdown) so
            // students aren't railroaded into `std_logic` when they want
            // `std_logic_vector(N downto 0)`, `unsigned(N downto 0)`,
            // `integer`, or any other VHDL type. F-1 fix (2026-06-28).
            { kind: 'text-field', name: 'TYPE', default: 'std_logic' }
        ]]
    });

    def('kmila_vhdl_signal_decl', {
        style: 'signal',
        prev: 'kmila_vhdl_decl',
        next: 'kmila_vhdl_decl',
        rows: [[
            { kind: 'label', text: 'signal' },
            { kind: 'text-field', name: 'NAME', default: 'q_int' },
            { kind: 'label', text: ':' },
            { kind: 'text-field', name: 'TYPE', default: 'std_logic' }
        ]]
    });

    /* ── Concurrent: assign + process ─────────────────────────────── */

    def('kmila_vhdl_assign_concurrent', {
        style: 'accent',
        prev: 'kmila_vhdl_concurrent',
        next: 'kmila_vhdl_concurrent',
        rows: [[
            { kind: 'text-field', name: 'TARGET', default: 'y' },
            { kind: 'label', text: '<=' },
            { kind: 'value', name: 'VALUE', check: 'kmila_vhdl_expr' }
        ]]
    });

    def('kmila_vhdl_process', {
        style: 'clock',
        prev: 'kmila_vhdl_concurrent',
        next: 'kmila_vhdl_concurrent',
        rows: [[
            { kind: 'label', text: 'process (' },
            { kind: 'text-field', name: 'SENS', default: 'clk, rst' },
            { kind: 'label', text: ')' }
        ]],
        statements: [
            { name: 'BODY', check: 'kmila_vhdl_seq', label: 'begin' }
        ]
    });

    /* ── Sequential: if / assign ──────────────────────────────────── */

    def('kmila_vhdl_if', {
        style: 'clock',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        rows: [[
            { kind: 'label', text: 'if' },
            { kind: 'value', name: 'COND', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: 'then' }
        ]],
        statements: [
            { name: 'THEN_BODY', check: 'kmila_vhdl_seq', label: 'then' },
            { name: 'ELSE_BODY', check: 'kmila_vhdl_seq', label: 'else' }
        ]
    });

    def('kmila_vhdl_assign_seq', {
        style: 'accent',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        rows: [[
            { kind: 'text-field', name: 'TARGET', default: 'q_int' },
            { kind: 'label', text: '<=' },
            { kind: 'value', name: 'VALUE', check: 'kmila_vhdl_expr' }
        ]]
    });

    // F-4 (2026-06-28) — case / when block family. Implements the
    // VHDL selector statement so multi-way branches no longer collapse
    // to one raw_seq tile. Three defs:
    //   • kmila_vhdl_case        — the `case <subj> is … end case;` outer
    //   • kmila_vhdl_case_when   — one alternative `when "00" => …`
    //   • kmila_vhdl_case_others — terminal `when others => …`
    def('kmila_vhdl_case', {
        style: 'clock',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        rows: [[
            { kind: 'label', text: 'case' },
            { kind: 'value', name: 'SUBJECT', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: 'is' }
        ]],
        statements: [
            { name: 'ALTS', check: 'kmila_vhdl_case_alt', label: 'alternatives' }
        ]
    });

    def('kmila_vhdl_case_when', {
        style: 'accent',
        prev: 'kmila_vhdl_case_alt',
        next: 'kmila_vhdl_case_alt',
        rows: [[
            { kind: 'label', text: 'when' },
            { kind: 'text-field', name: 'CHOICE', default: '"00"' },
            { kind: 'label', text: '=>' }
        ]],
        statements: [
            { name: 'BODY', check: 'kmila_vhdl_seq', label: 'do' }
        ]
    });

    def('kmila_vhdl_case_others', {
        style: 'accent',
        prev: 'kmila_vhdl_case_alt',
        next: 'kmila_vhdl_case_alt',
        rows: [[
            { kind: 'label', text: 'when others =>' }
        ]],
        statements: [
            { name: 'BODY', check: 'kmila_vhdl_seq', label: 'do' }
        ]
    });

    /* ── Expression atoms ─────────────────────────────────────────── */

    def('kmila_vhdl_id', {
        style: 'signal',
        output: 'kmila_vhdl_expr',
        rows: [[
            { kind: 'text-field', name: 'NAME', default: 'clk' }
        ]]
    });

    def('kmila_vhdl_literal_bit', {
        style: 'signal',
        output: 'kmila_vhdl_expr',
        rows: [[
            { kind: 'label', text: "'" },
            { kind: 'dropdown', name: 'BIT', options: [
                ["0", '0'],
                ["1", '1']
            ]},
            { kind: 'label', text: "'" }
        ]]
    });

    def('kmila_vhdl_eq', {
        style: 'gate',
        output: 'kmila_vhdl_expr',
        inline: true,
        rows: [[
            { kind: 'value', name: 'LHS', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: '=' },
            { kind: 'value', name: 'RHS', check: 'kmila_vhdl_expr' }
        ]]
    });

    def('kmila_vhdl_rising_edge', {
        style: 'gate',
        output: 'kmila_vhdl_expr',
        inline: true,
        rows: [[
            { kind: 'label', text: 'rising_edge(' },
            { kind: 'value', name: 'SIG', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: ')' }
        ]]
    });

    // F-7 (2026-06-28): falling_edge atom — symmetric clone of rising_edge
    // for async-reset / negative-edge designs that students hit in textbook
    // exercises (older flip-flop diagrams default to falling-edge).
    def('kmila_vhdl_falling_edge', {
        style: 'gate',
        output: 'kmila_vhdl_expr',
        inline: true,
        rows: [[
            { kind: 'label', text: 'falling_edge(' },
            { kind: 'value', name: 'SIG', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: ')' }
        ]]
    });

    def('kmila_vhdl_not', {
        style: 'gate',
        output: 'kmila_vhdl_expr',
        inline: true,
        rows: [[
            { kind: 'label', text: 'not' },
            { kind: 'value', name: 'OPERAND', check: 'kmila_vhdl_expr' }
        ]]
    });

    // F-2 (2026-06-28): generic binary boolean operator. Single block,
    // OP selector dropdown picks the keyword. Removes ~90 % of raw_expr
    // chips from typical combinational designs (`y <= a and b`,
    // `cond <= sel = "10" or rst`, …).
    def('kmila_vhdl_binop', {
        style: 'gate',
        output: 'kmila_vhdl_expr',
        inline: true,
        rows: [[
            { kind: 'value', name: 'LHS', check: 'kmila_vhdl_expr' },
            { kind: 'dropdown', name: 'OP', options: [
                ['and',  'and'],
                ['or',   'or'],
                ['xor',  'xor'],
                ['nand', 'nand'],
                ['nor',  'nor'],
                ['xnor', 'xnor']
            ]},
            { kind: 'value', name: 'RHS', check: 'kmila_vhdl_expr' }
        ]]
    });

    // F-5 (2026-06-28): vector / bit-string literal atom. Same surface as
    // literal_bit but accepts the multi-bit string form (`"00"`,
    // `B"1010"`, `X"FF"`). Round-trips verbatim — emitter just spits back
    // the TEXT field. Unlike raw_expr, the block has a recognisable label
    // and pill chrome so students see "this is a literal, not random text".
    def('kmila_vhdl_literal_vec', {
        style: 'signal',
        output: 'kmila_vhdl_expr',
        inline: true,
        rows: [[
            { kind: 'text-field', name: 'TEXT', default: '"00"' }
        ]]
    });

    /* ── Raw-VHDL fallback (Phase 0 round-trip safety) ────────────────
     * Four slot-shape variants so a piece of unsupported VHDL can survive
     * round-trip in whichever context the parser found it. Not surfaced
     * in the palette — created only by VhdlBlockBuilder when the source
     * contains constructs the block catalog doesn't cover yet. The
     * `TEXT` field carries the original VHDL verbatim; the emitter
     * spits it back out untouched at the appropriate indent.            */

    def('kmila_vhdl_raw_top', {
        style: 'raw',
        rows: [[
            { kind: 'label', text: 'VHDL' },
            { kind: 'text-field', name: 'TEXT', default: '' }
        ]]
    });

    def('kmila_vhdl_raw_concurrent', {
        style: 'raw',
        prev: 'kmila_vhdl_concurrent',
        next: 'kmila_vhdl_concurrent',
        rows: [[
            { kind: 'label', text: 'VHDL' },
            { kind: 'text-field', name: 'TEXT', default: '' }
        ]]
    });

    def('kmila_vhdl_raw_seq', {
        style: 'raw',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        rows: [[
            { kind: 'label', text: 'VHDL' },
            { kind: 'text-field', name: 'TEXT', default: '' }
        ]]
    });

    def('kmila_vhdl_raw_expr', {
        style: 'raw',
        output: 'kmila_vhdl_expr',
        inline: true,
        rows: [[
            { kind: 'text-field', name: 'TEXT', default: '' }
        ]]
    });
})();
