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

    // 2026-07-19 Phase 8 — optional GENERICS slot before PORTS. Emitter
    // renders `generic (…)` only when GENERICS chain is non-empty, so
    // pre-Phase-8 workspaces remain byte-identical.
    def('kmila_vhdl_entity', {
        style: 'clock',
        rows: [[
            { kind: 'label', text: 'entity' },
            { kind: 'text-field', name: 'NAME', placeholder: 'name', default: 'counter' },
            { kind: 'label', text: 'is' }
        ]],
        statements: [
            { name: 'GENERICS', check: 'kmila_vhdl_generic', label: 'generic' },
            { name: 'PORTS',    check: 'kmila_vhdl_port',    label: 'port' }
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
        // F-18 (2026-07-06 batch 2): optional INIT text field for the
        // `:= <init>` clause. Blank default so signals without an
        // initializer render as clean `signal name : type` — the
        // emitter suppresses the `:=` when INIT is empty.
        rows: [[
            { kind: 'label', text: 'signal' },
            { kind: 'text-field', name: 'NAME', default: 'q_int' },
            { kind: 'label', text: ':' },
            { kind: 'text-field', name: 'TYPE', default: 'std_logic' },
            { kind: 'label', text: ':=' },
            { kind: 'text-field', name: 'INIT', default: '' }
        ]]
    });

    // F-N enum type decl (2026-07-06 batch 2): `type state_t is (S_RED,
    // S_GREEN, S_YELLOW);`. NAME is the type name; MEMBERS holds a
    // chain of kmila_vhdl_enum_member blocks. Chains into the same
    // architecture DECLS slot as signal_decl.
    def('kmila_vhdl_type_enum', {
        style: 'signal',
        prev: 'kmila_vhdl_decl',
        next: 'kmila_vhdl_decl',
        rows: [[
            { kind: 'label', text: 'type' },
            { kind: 'text-field', name: 'NAME', default: 'state_t' },
            { kind: 'label', text: 'is (…)' }
        ]],
        statements: [
            { name: 'MEMBERS', check: 'kmila_vhdl_enum_member', label: 'members' }
        ]
    });

    def('kmila_vhdl_enum_member', {
        style: 'signal',
        prev: 'kmila_vhdl_enum_member',
        next: 'kmila_vhdl_enum_member',
        rows: [[
            { kind: 'text-field', name: 'NAME', default: 'S_RED' }
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

    // F-N conditional concurrent assign (2026-07-06 batch 2):
    //   target <= v1 when c1 else v2 when c2 else vN;
    // TARGET is the assigned signal; ARMS holds a chain of
    // kmila_vhdl_cond_arm blocks (one per `<value> when <cond>` pair);
    // ELSE_VALUE holds the tail expression rendered after the last
    // `else`. Blocks chain into the same slot as assign_concurrent.
    def('kmila_vhdl_cond_assign', {
        style: 'accent',
        prev: 'kmila_vhdl_concurrent',
        next: 'kmila_vhdl_concurrent',
        rows: [[
            { kind: 'text-field', name: 'TARGET', default: 'y' },
            { kind: 'label', text: '<= (conditional)' }
        ]],
        statements: [
            { name: 'ARMS', check: 'kmila_vhdl_cond_arm', label: 'arms' }
        ],
        // ELSE_VALUE is a value slot rendered inline in a second row.
        // Optional — omitted when the student wants an unconditional
        // tail (rare) but always emitted by the builder from real VHDL.
        moreRows: [[
            { kind: 'label', text: 'else' },
            { kind: 'value', name: 'ELSE_VALUE', check: 'kmila_vhdl_expr' }
        ]]
    });

    def('kmila_vhdl_cond_arm', {
        style: 'accent',
        prev: 'kmila_vhdl_cond_arm',
        next: 'kmila_vhdl_cond_arm',
        rows: [[
            { kind: 'value', name: 'VALUE', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: 'when' },
            { kind: 'value', name: 'COND',  check: 'kmila_vhdl_expr' }
        ]]
    });

    // 2026-07-19 Phase 4 (F-11): optional LABEL text-field. Blank default
    // preserves pre-Phase-4 workspace layouts (no visual change when
    // unlabeled). Emitter prefixes `<LABEL>:` and matches `end process
    // <LABEL>;` when LABEL is non-empty.
    // 2026-07-19 Phase 20 — DECLS chain for the process declarative part
    // (variables, constants, subprograms between `process[(sens)][is]` and
    // `begin`). Empty slot = pre-P20 shape emits identically (no `is`
    // keyword, no decls region). Populated slot triggers `process (…) is
    // <decls> begin` canonical form.
    def('kmila_vhdl_process', {
        style: 'clock',
        prev: 'kmila_vhdl_concurrent',
        next: 'kmila_vhdl_concurrent',
        rows: [[
            { kind: 'text-field', name: 'LABEL', default: '' },
            { kind: 'label', text: 'process (' },
            { kind: 'text-field', name: 'SENS', default: 'clk, rst' },
            { kind: 'label', text: ')' }
        ]],
        statements: [
            { name: 'DECLS', check: 'kmila_vhdl_decl', label: 'declarations' },
            { name: 'BODY',  check: 'kmila_vhdl_seq',  label: 'begin' }
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
            { name: 'ELSIF_CHAIN', check: 'kmila_vhdl_elsif', label: 'elsif' },
            { name: 'ELSE_BODY', check: 'kmila_vhdl_seq', label: 'else' }
        ]
    });

    // F-8 (2026-07-05) — native elsif arm. Chains linearly (prev/next of
    // its own type) inside the if-block's ELSIF_CHAIN slot, replacing the
    // pre-F-8 convention where elsif was encoded as a solitary if nested
    // in ELSE_BODY. The emitter still honours the old shape as a
    // fallback so pre-F-8 .bvhdl sidecars keep round-tripping.
    def('kmila_vhdl_elsif', {
        style: 'clock',
        prev: 'kmila_vhdl_elsif',
        next: 'kmila_vhdl_elsif',
        rows: [[
            { kind: 'label', text: 'elsif' },
            { kind: 'value', name: 'COND', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: 'then' }
        ]],
        statements: [
            { name: 'BODY', check: 'kmila_vhdl_seq', label: 'then' }
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

    /* ── Aggregates (2026-07-19 Phase 10) ─────────────────────────── */
    // General aggregate — parent block holding a chain of agg_elem
    // children. Handles positional `(a, b, c)`, named indexed
    // `(0 => '1', 2 => '1', others => '0')`, and mixed forms.
    def('kmila_vhdl_aggregate', {
        style: 'signal',
        output: 'kmila_vhdl_expr',
        rows: [[
            { kind: 'label', text: '(' }
        ]],
        statements: [
            { name: 'ELEMS', check: 'kmila_vhdl_agg_elem', label: 'elements' }
        ]
    });

    // One element of an aggregate. MODE picks the syntactic form:
    //   'positional' — `<value>` (CHOICE ignored)
    //   'indexed'    — `<choice> => <value>`
    //   'others'     — `others => <value>` (CHOICE ignored)
    def('kmila_vhdl_agg_elem', {
        style: 'accent',
        prev: 'kmila_vhdl_agg_elem',
        next: 'kmila_vhdl_agg_elem',
        inline: true,
        rows: [[
            { kind: 'dropdown', name: 'MODE', options: [
                ['positional', 'positional'],
                ['indexed',    'indexed'],
                ['others',     'others']
            ]},
            { kind: 'text-field', name: 'CHOICE', default: '' },
            { kind: 'label', text: '=>' },
            { kind: 'value', name: 'VALUE', check: 'kmila_vhdl_expr' }
        ]]
    });

    /* ── Top-level (2026-07-19 Phase 8) ───────────────────────────── */
    // library <NAMES>;   (comma-separated library identifiers)
    def('kmila_vhdl_library', {
        style: 'signal',
        rows: [[
            { kind: 'label', text: 'library' },
            { kind: 'text-field', name: 'NAMES', default: 'ieee' }
        ]]
    });

    // use <PATH>;
    def('kmila_vhdl_use', {
        style: 'signal',
        rows: [[
            { kind: 'label', text: 'use' },
            { kind: 'text-field', name: 'PATH', default: 'ieee.std_logic_1164.all' }
        ]]
    });

    // Entity generic: <NAME> : <TYPE> [ := <DEFAULT> ]
    def('kmila_vhdl_generic', {
        style: 'accent',
        prev: 'kmila_vhdl_generic',
        next: 'kmila_vhdl_generic',
        rows: [[
            { kind: 'text-field', name: 'NAME', default: 'N' },
            { kind: 'label', text: ':' },
            { kind: 'text-field', name: 'TYPE', default: 'integer' },
            { kind: 'label', text: ':=' },
            { kind: 'text-field', name: 'DEFAULT', default: '' }
        ]]
    });

    // package <NAME> is … end package [<NAME>];
    def('kmila_vhdl_package', {
        style: 'clock',
        rows: [[
            { kind: 'label', text: 'package' },
            { kind: 'text-field', name: 'NAME', default: 'my_pkg' },
            { kind: 'label', text: 'is' }
        ]],
        statements: [
            { name: 'DECLS', check: 'kmila_vhdl_decl', label: 'declarations' }
        ]
    });

    // package body <NAME> is … end package body [<NAME>];
    def('kmila_vhdl_package_body', {
        style: 'clock',
        rows: [[
            { kind: 'label', text: 'package body' },
            { kind: 'text-field', name: 'NAME', default: 'my_pkg' },
            { kind: 'label', text: 'is' }
        ]],
        statements: [
            { name: 'BODY', check: 'kmila_vhdl_decl', label: 'body' }
        ]
    });

    /* ── Tier D declarations (2026-07-19 Phase 7) ─────────────────── */
    // Every construct that used to be silently dropped from the DECLS
    // section now has a first-class block (or lands in raw_decl as a
    // labeled safety net — never invisible).

    // constant NAME : TYPE := VALUE;
    def('kmila_vhdl_constant', {
        style: 'signal',
        prev: 'kmila_vhdl_decl',
        next: 'kmila_vhdl_decl',
        rows: [[
            { kind: 'label', text: 'constant' },
            { kind: 'text-field', name: 'NAME', default: 'K' },
            { kind: 'label', text: ':' },
            { kind: 'text-field', name: 'TYPE', default: 'integer' },
            { kind: 'label', text: ':=' },
            { kind: 'text-field', name: 'VALUE', default: '0' }
        ]]
    });

    // subtype NAME is TYPE_EXPR;
    def('kmila_vhdl_subtype', {
        style: 'signal',
        prev: 'kmila_vhdl_decl',
        next: 'kmila_vhdl_decl',
        rows: [[
            { kind: 'label', text: 'subtype' },
            { kind: 'text-field', name: 'NAME', default: 'S' },
            { kind: 'label', text: 'is' },
            { kind: 'text-field', name: 'TYPE_EXPR', default: 'integer range 0 to 7' }
        ]]
    });

    // alias NAME is TARGET;
    def('kmila_vhdl_alias', {
        style: 'signal',
        prev: 'kmila_vhdl_decl',
        next: 'kmila_vhdl_decl',
        rows: [[
            { kind: 'label', text: 'alias' },
            { kind: 'text-field', name: 'NAME', default: 'a' },
            { kind: 'label', text: 'is' },
            { kind: 'text-field', name: 'TARGET', default: 'x(0)' }
        ]]
    });

    // type NAME is array (RANGE) of ELEM_TYPE;
    def('kmila_vhdl_type_array', {
        style: 'signal',
        prev: 'kmila_vhdl_decl',
        next: 'kmila_vhdl_decl',
        rows: [[
            { kind: 'label', text: 'type' },
            { kind: 'text-field', name: 'NAME', default: 'mem_t' },
            { kind: 'label', text: 'is array (' },
            { kind: 'text-field', name: 'RANGE', default: '0 to 15' },
            { kind: 'label', text: ') of' },
            { kind: 'text-field', name: 'ELEM_TYPE', default: 'std_logic_vector(7 downto 0)' }
        ]]
    });

    // type NAME is record … end record;
    def('kmila_vhdl_type_record', {
        style: 'signal',
        prev: 'kmila_vhdl_decl',
        next: 'kmila_vhdl_decl',
        rows: [[
            { kind: 'label', text: 'type' },
            { kind: 'text-field', name: 'NAME', default: 'bus_t' },
            { kind: 'label', text: 'is record' }
        ]],
        statements: [
            { name: 'FIELDS', check: 'kmila_vhdl_record_field', label: 'fields' }
        ]
    });

    def('kmila_vhdl_record_field', {
        style: 'accent',
        prev: 'kmila_vhdl_record_field',
        next: 'kmila_vhdl_record_field',
        rows: [[
            { kind: 'text-field', name: 'NAME', default: 'f' },
            { kind: 'label', text: ':' },
            { kind: 'text-field', name: 'TYPE', default: 'std_logic' }
        ]]
    });

    // variable NAME : TYPE [ := INIT ];   (process-scope variable declaration)
    def('kmila_vhdl_variable_decl', {
        style: 'signal',
        prev: 'kmila_vhdl_decl',
        next: 'kmila_vhdl_decl',
        rows: [[
            { kind: 'label', text: 'variable' },
            { kind: 'text-field', name: 'NAME', default: 'v' },
            { kind: 'label', text: ':' },
            { kind: 'text-field', name: 'TYPE', default: 'integer' },
            { kind: 'label', text: ':=' },
            { kind: 'text-field', name: 'INIT', default: '' }
        ]]
    });

    // variable-assign `v := expr;` (sequential body, mirror of assign_seq)
    def('kmila_vhdl_variable_assign', {
        style: 'accent',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        rows: [[
            { kind: 'text-field', name: 'TARGET', default: 'v' },
            { kind: 'label', text: ':=' },
            { kind: 'value', name: 'VALUE', check: 'kmila_vhdl_expr' }
        ]]
    });

    // attribute NAME : TYPE;
    def('kmila_vhdl_attribute_decl', {
        style: 'raw',
        prev: 'kmila_vhdl_decl',
        next: 'kmila_vhdl_decl',
        rows: [[
            { kind: 'label', text: 'attribute' },
            { kind: 'text-field', name: 'NAME', default: 'a' },
            { kind: 'label', text: ':' },
            { kind: 'text-field', name: 'TYPE', default: 'string' }
        ]]
    });

    // attribute NAME of TARGET : CLASS is VALUE;
    def('kmila_vhdl_attribute_spec', {
        style: 'raw',
        prev: 'kmila_vhdl_decl',
        next: 'kmila_vhdl_decl',
        rows: [[
            { kind: 'label', text: 'attribute' },
            { kind: 'text-field', name: 'NAME', default: 'a' },
            { kind: 'label', text: 'of' },
            { kind: 'text-field', name: 'TARGET', default: 'x' },
            { kind: 'label', text: ':' },
            { kind: 'text-field', name: 'CLASS', default: 'signal' },
            { kind: 'label', text: 'is' },
            { kind: 'text-field', name: 'VALUE', default: '"…"' }
        ]]
    });

    // Safety-net for anything else that appears in a DECLS run — never
    // silently dropped.
    def('kmila_vhdl_raw_decl', {
        style: 'raw',
        prev: 'kmila_vhdl_decl',
        next: 'kmila_vhdl_decl',
        rows: [[
            { kind: 'text-field', name: 'TEXT', default: '' }
        ]]
    });

    /* ── Subprograms (2026-07-19 Phase 19) ────────────────────────── */
    // procedure <NAME>(<params>) is <decls> begin <body> end [procedure|<NAME>];
    // Two chains inside the block: DECLS (kmila_vhdl_decl, for locals like
    // variable_decl, constant, subtype) and BODY (kmila_vhdl_seq, mirror of
    // process body). PARAMS chain uses kmila_vhdl_param wrappers.
    def('kmila_vhdl_procedure_decl', {
        style: 'clock',
        prev: 'kmila_vhdl_decl',
        next: 'kmila_vhdl_decl',
        rows: [[
            { kind: 'label', text: 'procedure' },
            { kind: 'text-field', name: 'NAME', default: 'my_proc' }
        ]],
        statements: [
            { name: 'PARAMS', check: 'kmila_vhdl_param', label: 'params' },
            { name: 'DECLS',  check: 'kmila_vhdl_decl',  label: 'declarations' },
            { name: 'BODY',   check: 'kmila_vhdl_seq',   label: 'begin' }
        ]
    });

    // function <NAME>(<params>) return <TYPE> is <decls> begin <body> end [function|<NAME>];
    def('kmila_vhdl_function_decl', {
        style: 'clock',
        prev: 'kmila_vhdl_decl',
        next: 'kmila_vhdl_decl',
        rows: [[
            { kind: 'label', text: 'function' },
            { kind: 'text-field', name: 'NAME', default: 'my_func' },
            { kind: 'label', text: 'return' },
            { kind: 'text-field', name: 'RETURN_TYPE', default: 'std_logic' }
        ]],
        statements: [
            { name: 'PARAMS', check: 'kmila_vhdl_param', label: 'params' },
            { name: 'DECLS',  check: 'kmila_vhdl_decl',  label: 'declarations' },
            { name: 'BODY',   check: 'kmila_vhdl_seq',   label: 'begin' }
        ]
    });

    // Subprogram parameter. CLASS defaults to blank (semantically `constant`
    // per LRM §4.2.2.1 for mode `in`; emitter omits when blank). MODE
    // defaults to `in`.
    def('kmila_vhdl_param', {
        style: 'accent',
        prev: 'kmila_vhdl_param',
        next: 'kmila_vhdl_param',
        inline: true,
        rows: [[
            { kind: 'dropdown', name: 'CLASS', options: [
                ['(default)', ''],
                ['constant',  'constant'],
                ['variable',  'variable'],
                ['signal',    'signal']
            ]},
            { kind: 'text-field', name: 'NAME', default: 'a' },
            { kind: 'label', text: ':' },
            { kind: 'dropdown', name: 'MODE', options: [
                ['in',    'in'],
                ['out',   'out'],
                ['inout', 'inout']
            ]},
            { kind: 'text-field', name: 'TYPE', default: 'std_logic' }
        ]]
    });

    /* ── with … select (2026-07-19 Phase 6) ────────────────────────── */
    // with <subj> select <target> <= <v1> when <c1>, <v2> when <c2>,
    //                                …, <vN> when others;
    def('kmila_vhdl_with_select', {
        style: 'clock',
        prev: 'kmila_vhdl_concurrent',
        next: 'kmila_vhdl_concurrent',
        inline: true,
        rows: [[
            { kind: 'label', text: 'with' },
            { kind: 'value', name: 'SUBJECT', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: 'select' },
            { kind: 'text-field', name: 'TARGET', default: 'y' },
            { kind: 'label', text: '<=' }
        ]],
        statements: [
            { name: 'ALTS', check: 'kmila_vhdl_with_alt', label: 'alternatives' }
        ]
    });

    def('kmila_vhdl_with_when', {
        style: 'accent',
        prev: 'kmila_vhdl_with_alt',
        next: 'kmila_vhdl_with_alt',
        inline: true,
        rows: [[
            { kind: 'value', name: 'VALUE', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: 'when' },
            { kind: 'text-field', name: 'CHOICE', default: '"00"' }
        ]]
    });

    def('kmila_vhdl_with_others', {
        style: 'accent',
        prev: 'kmila_vhdl_with_alt',
        next: 'kmila_vhdl_with_alt',
        inline: true,
        rows: [[
            { kind: 'value', name: 'VALUE', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: 'when others' }
        ]]
    });

    /* ── Labeled concurrent statements (2026-07-19 Phase 4 / F-11) ─── */

    // <label>: <component> [generic map (…)] port map (…);
    // Statement slots hold kmila_vhdl_port_assoc chains for both maps.
    def('kmila_vhdl_component_inst', {
        style: 'clock',
        prev: 'kmila_vhdl_concurrent',
        next: 'kmila_vhdl_concurrent',
        rows: [[
            { kind: 'text-field', name: 'LABEL', default: 'u1' },
            { kind: 'label', text: ':' },
            { kind: 'text-field', name: 'COMPONENT', default: 'my_component' }
        ]],
        statements: [
            { name: 'GENERIC_MAP', check: 'kmila_vhdl_port_assoc', label: 'generic map' },
            { name: 'PORT_MAP',    check: 'kmila_vhdl_port_assoc', label: 'port map' }
        ]
    });

    // 2026-07-19 Phase 19 — component declaration (arch/package DECLS
    // slot). Distinct from Phase 4's kmila_vhdl_component_inst which is
    // the body-level instantiation with LABEL + maps. Testbenches
    // declare their UUT interface here before instantiating it below.
    // GENERICS optional (rare in student designs but supported).
    def('kmila_vhdl_component_decl', {
        style: 'clock',
        prev: 'kmila_vhdl_decl',
        next: 'kmila_vhdl_decl',
        rows: [[
            { kind: 'label', text: 'component' },
            { kind: 'text-field', name: 'NAME', default: 'my_component' }
        ]],
        statements: [
            { name: 'GENERICS', check: 'kmila_vhdl_generic', label: 'generic' },
            { name: 'PORTS',    check: 'kmila_vhdl_port',    label: 'port' }
        ]
    });

    // <formal> => <actual>   (association wrapper — shared by generic map + port map)
    def('kmila_vhdl_port_assoc', {
        style: 'accent',
        prev: 'kmila_vhdl_port_assoc',
        next: 'kmila_vhdl_port_assoc',
        inline: true,
        rows: [[
            { kind: 'text-field', name: 'FORMAL', default: 'a' },
            { kind: 'label', text: '=>' },
            { kind: 'value', name: 'ACTUAL', check: 'kmila_vhdl_expr' }
        ]]
    });

    /* ── Generate statements (2026-07-19 Phase 5 / F-7) ─────────────── */

    // <label>: for <var> in <lo> (to|downto) <hi> generate … end generate [<label>];
    def('kmila_vhdl_generate_for', {
        style: 'clock',
        prev: 'kmila_vhdl_concurrent',
        next: 'kmila_vhdl_concurrent',
        inline: true,
        rows: [[
            { kind: 'text-field', name: 'LABEL', default: 'gen' },
            { kind: 'label', text: ': for' },
            { kind: 'text-field', name: 'VAR', default: 'i' },
            { kind: 'label', text: 'in' },
            { kind: 'value', name: 'LO', check: 'kmila_vhdl_expr' },
            { kind: 'dropdown', name: 'DIR', options: [['to','to'],['downto','downto']] },
            { kind: 'value', name: 'HI', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: 'generate' }
        ]],
        statements: [
            { name: 'BODY', check: 'kmila_vhdl_concurrent', label: 'do' }
        ]
    });

    // <label>: if <cond> generate … [else generate …] end generate [<label>];
    def('kmila_vhdl_generate_if', {
        style: 'clock',
        prev: 'kmila_vhdl_concurrent',
        next: 'kmila_vhdl_concurrent',
        inline: true,
        rows: [[
            { kind: 'text-field', name: 'LABEL', default: 'g_if' },
            { kind: 'label', text: ': if' },
            { kind: 'value', name: 'COND', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: 'generate' }
        ]],
        statements: [
            { name: 'THEN_BODY', check: 'kmila_vhdl_concurrent', label: 'then' },
            { name: 'ELSE_BODY', check: 'kmila_vhdl_concurrent', label: 'else' }
        ]
    });

    /* ── Sequential single-statement primitives (2026-07-19 1:1 audit) ── */

    // wait [ until <cond> | for <time> | on <sig>[, <sig>…] ] ;
    def('kmila_vhdl_wait', {
        style: 'clock',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        inline: true,
        rows: [[
            { kind: 'label', text: 'wait' },
            { kind: 'dropdown', name: 'MODE', options: [
                ['(bare)', ''],
                ['until', 'until'],
                ['for', 'for'],
                ['on', 'on']
            ]},
            { kind: 'text-field', name: 'EXPR', default: '' }
        ]]
    });

    // exit [ <label> ] [ when <cond> ] ;
    def('kmila_vhdl_exit', {
        style: 'accent',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        inline: true,
        rows: [[
            { kind: 'label', text: 'exit' },
            { kind: 'text-field', name: 'LABEL', default: '' },
            { kind: 'label', text: 'when' },
            { kind: 'text-field', name: 'COND', default: '' }
        ]]
    });

    // next [ <label> ] [ when <cond> ] ;
    def('kmila_vhdl_next', {
        style: 'accent',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        inline: true,
        rows: [[
            { kind: 'label', text: 'next' },
            { kind: 'text-field', name: 'LABEL', default: '' },
            { kind: 'label', text: 'when' },
            { kind: 'text-field', name: 'COND', default: '' }
        ]]
    });

    // return [ <expr> ] ;   (permitted inside functions/procedures)
    def('kmila_vhdl_return', {
        style: 'accent',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        inline: true,
        rows: [[
            { kind: 'label', text: 'return' },
            { kind: 'text-field', name: 'EXPR', default: '' }
        ]]
    });

    // null ;
    def('kmila_vhdl_null', {
        style: 'signal',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        rows: [[
            { kind: 'label', text: 'null' }
        ]]
    });

    // report "<msg>" [ severity <level> ] ;
    def('kmila_vhdl_report', {
        style: 'gate',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        inline: true,
        rows: [[
            { kind: 'label', text: 'report' },
            { kind: 'text-field', name: 'MSG', default: '"…"' },
            { kind: 'label', text: 'severity' },
            { kind: 'dropdown', name: 'SEVERITY', options: [
                ['(none)', ''],
                ['note', 'note'],
                ['warning', 'warning'],
                ['error', 'error'],
                ['failure', 'failure']
            ]}
        ]]
    });

    // assert <cond> [ report "<msg>" ] [ severity <level> ] ;
    def('kmila_vhdl_assert', {
        style: 'gate',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        inline: true,
        rows: [[
            { kind: 'label', text: 'assert' },
            { kind: 'value', name: 'COND', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: 'report' },
            { kind: 'text-field', name: 'MSG', default: '""' },
            { kind: 'label', text: 'severity' },
            { kind: 'dropdown', name: 'SEVERITY', options: [
                ['(none)', ''],
                ['note', 'note'],
                ['warning', 'warning'],
                ['error', 'error'],
                ['failure', 'failure']
            ]}
        ]]
    });

    // 2026-07-19 Phase 19 — procedure call as a SEQUENTIAL statement:
    //   printmsg("Set Reset");
    //   check_and_gate('0', '0', '0');
    // Distinct from kmila_vhdl_call (which is an EXPRESSION producing a
    // value on the RHS of `:=`/`<=`). Placed AFTER assign-seq detection in
    // the builder so `x := f(a)` still binds as an assign. Reuses the
    // existing kmila_vhdl_call_arg wrapper for positional args.
    def('kmila_vhdl_procedure_call', {
        style: 'accent',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        rows: [[
            { kind: 'text-field', name: 'NAME', default: 'printmsg' },
            { kind: 'label', text: '(…)' }
        ]],
        statements: [
            { name: 'ARGS', check: 'kmila_vhdl_call_arg', label: 'args' }
        ]
    });

    /* ── Loops (2026-07-19 1:1 audit Phase 3) ─────────────────────── */

    // for <var> in <lo> (to|downto) <hi> loop … end loop;
    def('kmila_vhdl_for_loop', {
        style: 'clock',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        inline: true,
        rows: [[
            { kind: 'label', text: 'for' },
            { kind: 'text-field', name: 'VAR', default: 'i' },
            { kind: 'label', text: 'in' },
            { kind: 'value', name: 'LO', check: 'kmila_vhdl_expr' },
            { kind: 'dropdown', name: 'DIR', options: [['to','to'],['downto','downto']] },
            { kind: 'value', name: 'HI', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: 'loop' }
        ]],
        statements: [
            { name: 'BODY', check: 'kmila_vhdl_seq', label: 'do' }
        ]
    });

    // while <cond> loop … end loop;
    def('kmila_vhdl_while_loop', {
        style: 'clock',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        inline: true,
        rows: [[
            { kind: 'label', text: 'while' },
            { kind: 'value', name: 'COND', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: 'loop' }
        ]],
        statements: [
            { name: 'BODY', check: 'kmila_vhdl_seq', label: 'do' }
        ]
    });

    // loop … end loop;   (typically paired with `exit`)
    def('kmila_vhdl_loop', {
        style: 'clock',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        rows: [[
            { kind: 'label', text: 'loop' }
        ]],
        statements: [
            { name: 'BODY', check: 'kmila_vhdl_seq', label: 'do' }
        ]
    });

    /* ── Comments (2026-07-19 1:1 audit Phase 9) ─────────────────── */

    // Four variant blocks, one per slot. Each carries a single TEXT
    // field with the comment body (without the leading `-- `). Style is
    // `raw` so the block reads visually as a preserved fragment, not a
    // first-class construct.
    def('kmila_vhdl_comment_top', {
        style: 'raw',
        rows: [[
            { kind: 'label', text: '--' },
            { kind: 'text-field', name: 'TEXT', default: 'comment' }
        ]]
    });

    def('kmila_vhdl_comment_decl', {
        style: 'raw',
        prev: 'kmila_vhdl_decl',
        next: 'kmila_vhdl_decl',
        rows: [[
            { kind: 'label', text: '--' },
            { kind: 'text-field', name: 'TEXT', default: 'comment' }
        ]]
    });

    def('kmila_vhdl_comment_concurrent', {
        style: 'raw',
        prev: 'kmila_vhdl_concurrent',
        next: 'kmila_vhdl_concurrent',
        rows: [[
            { kind: 'label', text: '--' },
            { kind: 'text-field', name: 'TEXT', default: 'comment' }
        ]]
    });

    def('kmila_vhdl_comment_seq', {
        style: 'raw',
        prev: 'kmila_vhdl_seq',
        next: 'kmila_vhdl_seq',
        rows: [[
            { kind: 'label', text: '--' },
            { kind: 'text-field', name: 'TEXT', default: 'comment' }
        ]]
    });

    /* ── Expression atoms ─────────────────────────────────────────── */

    def('kmila_vhdl_id', {
        style: 'signal',
        output: 'kmila_vhdl_expr',
        rows: [[
            { kind: 'text-field', name: 'NAME', default: 'clk' }
        ]]
    });

    // 2026-07-19 1:1 audit — full std_ulogic alphabet (LRM §14.2):
    //   0/1 = forcing low/high · X = forcing unknown · Z = high impedance
    //   U = uninitialised    · L/H = weak low/high  · W = weak unknown
    //   -  = don't care (case-others arms)
    def('kmila_vhdl_literal_bit', {
        style: 'signal',
        output: 'kmila_vhdl_expr',
        rows: [[
            { kind: 'label', text: "'" },
            { kind: 'dropdown', name: 'BIT', options: [
                ["0", '0'],
                ["1", '1'],
                ["X", 'X'],
                ["Z", 'Z'],
                ["U", 'U'],
                ["-", '-'],
                ["L", 'L'],
                ["H", 'H'],
                ["W", 'W']
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

    // F-2 (2026-06-28): generic binary boolean operator. F-9 (2026-07-05)
    // widened the OP dropdown to cover arithmetic (+, -, *, /, mod, rem)
    // and non-equality relational (/=, <, >, <=, >=). Single block, single
    // OP selector — removes ~90 % of raw_expr chips from typical designs
    // and gives students an arithmetic surface (adders, counters).
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
                ['xnor', 'xnor'],
                ['+',    '+'],
                ['-',    '-'],
                ['*',    '*'],
                ['/',    '/'],
                ['**',   '**'],
                ['mod',  'mod'],
                ['rem',  'rem'],
                ['/=',   '/='],
                ['<',    '<'],
                ['>',    '>'],
                ['<=',   '<='],
                ['>=',   '>=']
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

    // F-10 (2026-07-05): vector slice `sig(hi downto lo)`. SIG names the
    // vector, DIR toggles downto/to, HI/LO are expression inputs.
    def('kmila_vhdl_slice', {
        style: 'signal',
        output: 'kmila_vhdl_expr',
        inline: true,
        rows: [[
            { kind: 'text-field', name: 'SIG', default: 'sig' },
            { kind: 'label', text: '(' },
            { kind: 'value', name: 'HI', check: 'kmila_vhdl_expr' },
            { kind: 'dropdown', name: 'DIR', options: [['downto', 'downto'], ['to', 'to']] },
            { kind: 'value', name: 'LO', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: ')' }
        ]]
    });

    // F-10 (2026-07-05): scalar index `sig(i)` — memory read / bus tap.
    def('kmila_vhdl_index', {
        style: 'signal',
        output: 'kmila_vhdl_expr',
        inline: true,
        rows: [[
            { kind: 'text-field', name: 'SIG', default: 'sig' },
            { kind: 'label', text: '(' },
            { kind: 'value', name: 'IDX', check: 'kmila_vhdl_expr' },
            { kind: 'label', text: ')' }
        ]]
    });

    // F-N call (2026-07-06 batch 2) — function/type-conversion call
    // `name(a1, a2, …)`. NAME is the function identifier; ARGS holds a
    // chain of kmila_vhdl_call_arg wrappers, each carrying one
    // sub-expression. Runs alongside the slice/index atoms in the
    // expression tier of the toolbox.
    def('kmila_vhdl_call', {
        style: 'signal',
        output: 'kmila_vhdl_expr',
        rows: [[
            { kind: 'text-field', name: 'NAME', default: 'to_unsigned' },
            { kind: 'label', text: '(…)' }
        ]],
        statements: [
            { name: 'ARGS', check: 'kmila_vhdl_call_arg', label: 'args' }
        ]
    });

    def('kmila_vhdl_call_arg', {
        style: 'signal',
        prev: 'kmila_vhdl_call_arg',
        next: 'kmila_vhdl_call_arg',
        inline: true,
        rows: [[
            { kind: 'value', name: 'VALUE', check: 'kmila_vhdl_expr' }
        ]]
    });

    // F-12 (2026-07-05): `(others => '0')` aggregate. VAL is a text field
    // (holds the RHS literal verbatim including quotes) rather than a
    // sub-expression, matching how students actually write these.
    def('kmila_vhdl_others', {
        style: 'signal',
        output: 'kmila_vhdl_expr',
        inline: true,
        rows: [[
            { kind: 'label', text: '(others =>' },
            { kind: 'text-field', name: 'VAL', default: "'0'" },
            { kind: 'label', text: ')' }
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
