// VHDL language documentation — English.
//
// Consumed by vhdl.js (hover, completion, definition, references, doc-symbols,
// signature-help providers). Other languages live alongside this file and fall
// back to English by merging on top of this object in vhdl.js.
//
// Structure:
//   keywords  : { name → { summary, description, example? } }
//   types     : { name → { summary, description, example? } }
//   builtins  : { name → { summary, signature, description, example? } }
//   snippets  : { name → { label, summary, insertText } }   (Monaco snippet syntax)
//   ui        : { key → translated short strings used by the providers themselves }

(function () {
    window.kmilaVhdlDocs = window.kmilaVhdlDocs || {};
    window.kmilaVhdlDocs.en = {

        keywords: {
            // --- Structural declarations ---
            entity:       { summary: "Declares a hardware module's external interface.", description: "An **entity** names a design unit and lists its ports (inputs/outputs) and generics (parameters). It is the black-box view — an architecture supplies the implementation.", example: "entity counter is\n  port (\n    clk : in  std_logic;\n    q   : out std_logic_vector(7 downto 0)\n  );\nend entity counter;" },
            architecture: { summary: "Implementation body for an entity.", description: "An **architecture** supplies one possible implementation for an entity. A single entity can have multiple architectures (e.g. `behavioral`, `rtl`, `structural`).", example: "architecture behavioral of counter is\n  signal cnt : unsigned(7 downto 0) := (others => '0');\nbegin\n  process(clk)\n  begin\n    if rising_edge(clk) then cnt <= cnt + 1; end if;\n  end process;\n  q <= std_logic_vector(cnt);\nend architecture behavioral;" },
            component:    { summary: "Instantiable copy of an entity's interface.", description: "A **component** declaration lets an architecture instantiate another entity. Required before modern direct-entity instantiation.", example: "component fifo is\n  port ( clk : in std_logic; din : in std_logic_vector(7 downto 0) );\nend component;" },
            configuration:{ summary: "Binds a component to a specific entity/architecture pair.", description: "Rarely used in synthesisable code; mostly for simulation where multiple architectures exist for one entity." },
            package:      { summary: "Collection of reusable types, constants, and subprograms.", description: "A **package** groups declarations that can be imported into other design units via `use`.", example: "package my_pkg is\n  constant WIDTH : integer := 8;\n  type state_t is (idle, busy, done);\nend package my_pkg;" },
            body:         { summary: "Implementation half of a package.", description: "`package body` contains the implementations of the subprograms declared in `package`." },

            // --- Libraries & use clauses ---
            library:      { summary: "Makes a library visible.", description: "Imports a VHDL library. `ieee` is the one you almost always want for `std_logic_1164` and `numeric_std`.", example: "library ieee;" },
            use:          { summary: "Makes package contents visible.", description: "Imports names from a package so you can use them without qualifying.", example: "use ieee.std_logic_1164.all;\nuse ieee.numeric_std.all;" },

            // --- Ports & generics ---
            port:         { summary: "Declares entity ports (signals crossing the boundary).", description: "Appears inside an entity. Each port has a name, direction (`in`, `out`, `inout`, `buffer`) and type.", example: "port (\n  clk  : in  std_logic;\n  data : out std_logic_vector(7 downto 0)\n);" },
            generic:      { summary: "Declares compile-time parameters for an entity.", description: "Generics are like template parameters — the instantiator picks their value, and the design scales (e.g. bus widths, FIFO depths).", example: "generic ( WIDTH : integer := 8 );" },
            map:          { summary: "Connects instance ports/generics to the surrounding signals.", description: "Used in `port map (...)` and `generic map (...)` on component instantiations.", example: "u_counter : counter port map (\n  clk => sys_clk,\n  q   => led\n);" },
            in:           { summary: "Port direction — input only.", description: "The design reads this port; it cannot drive it." },
            out:          { summary: "Port direction — output only.", description: "The design drives this port; its value cannot be read back inside the same entity (use `buffer` or an internal signal if you need to)." },
            inout:        { summary: "Bidirectional port.", description: "Drives and reads from the same physical wire. Only one driver may be active at a time — the others must be `'Z'` (high-impedance)." },
            buffer:       { summary: "Output that can also be read internally.", description: "Like `out`, but also readable inside the entity. Considered dated — prefer `out` with an internal signal." },

            // --- Signal declarations ---
            signal:       { summary: "Declares a signal (wire).", description: "A **signal** carries values through time. Inside a process, signal assignments (`<=`) schedule the update rather than apply it immediately.", example: "signal counter : unsigned(7 downto 0) := (others => '0');" },
            variable:     { summary: "Declares a variable (process-local).", description: "A **variable** lives inside a process or subprogram. Unlike a signal, `:=` assigns immediately — useful for intermediate computation within a process.", example: "process\n  variable tmp : integer := 0;\nbegin\n  tmp := tmp + 1;\nend process;" },
            constant:     { summary: "Declares a named constant.", description: "Gives a readable name to a fixed value. Can appear at any scope.", example: "constant CLOCK_HZ : integer := 50_000_000;" },
            shared:       { summary: "Shared variable accessible to multiple processes.", description: "Only meaningful in VHDL-2008. Use carefully — typically protected types or integers." },

            // --- Type system ---
            type:         { summary: "Defines a new type.", description: "Used for enumerations, ranges, arrays, records.", example: "type state_t is (idle, run, done);" },
            subtype:      { summary: "Declares a constrained version of an existing type.", description: "Adds range or resolution constraints without creating a fundamentally new type.", example: "subtype byte is std_logic_vector(7 downto 0);" },
            array:        { summary: "Declares an array type.", description: "One- or multi-dimensional. Can be constrained (fixed size) or unconstrained (size fixed when used).", example: "type memory_t is array(0 to 255) of std_logic_vector(7 downto 0);" },
            record:       { summary: "Declares a composite type with named fields.", description: "VHDL's equivalent of C structs.", example: "type axi_cmd_t is record\n  addr : std_logic_vector(31 downto 0);\n  len  : unsigned(7 downto 0);\nend record;" },
            range:        { summary: "Range constraint for scalar types.", description: "Typically `A to B` (ascending) or `A downto B` (descending). Also used in for-loops.", example: "for i in 0 to 7 loop ... end loop;" },
            downto:       { summary: "Descending range direction.", description: "`MSB downto LSB` — standard way to declare vector indices so bit 7 is on the left.", example: "std_logic_vector(7 downto 0)" },
            to:           { summary: "Ascending range direction.", description: "`A to B` — used in loops and upward-indexed arrays.", example: "for i in 0 to 15 loop ... end loop;" },
            others:       { summary: "Wildcard for aggregate defaults.", description: "Sets all un-named positions in an aggregate.", example: "signal zeros : std_logic_vector(7 downto 0) := (others => '0');" },
            units:        { summary: "Declares the units of a physical type.", description: "Used when defining custom physical types like `time`." },
            access:       { summary: "Pointer-like access type.", description: "Rarely synthesisable — used for linked-list data structures in simulation." },
            file:         { summary: "File type for I/O during simulation.", description: "Not synthesisable. Common in test benches for reading stimulus files." },

            // --- Sequential statements / control flow ---
            process:      { summary: "Sequential block of statements.", description: "A **process** executes its statements sequentially and re-runs whenever a signal in its sensitivity list changes. The bread-and-butter of clocked logic.", example: "process(clk, reset)\nbegin\n  if reset = '1' then\n    q <= (others => '0');\n  elsif rising_edge(clk) then\n    q <= d;\n  end if;\nend process;" },
            if:           { summary: "Conditional execution.", description: "Standard `if … then … elsif … else … end if;` structure inside a process.", example: "if rising_edge(clk) then\n  q <= d;\nend if;" },
            then:         { summary: "Separates the condition from the body in `if` / `elsif`.", description: "" },
            elsif:        { summary: "Additional conditional branch in an `if` statement.", description: "" },
            else:         { summary: "Fallback branch in `if` / `when`.", description: "" },
            case:         { summary: "Multi-branch based on a value.", description: "Like `switch` in C. Often cleaner than a long `if`/`elsif` chain.", example: "case state is\n  when idle => …\n  when busy => …\n  when others => null;\nend case;" },
            when:         { summary: "Branch selector in `case`, also used in conditional signal assignment.", description: "", example: "q <= '1' when enable = '1' else '0';" },
            select:       { summary: "Selects among alternatives in a selected signal assignment.", description: "Concurrent `with … select` statement, one of the two classic concurrent conditional forms.", example: "with state select\n  q <= a when idle,\n       b when busy,\n       c when others;" },
            loop:         { summary: "Generic loop construct.", description: "Can be unconditional (infinite), `for`-indexed, or `while`-conditioned.", example: "for i in 0 to 7 loop\n  result(i) <= data(7 - i);\nend loop;" },
            for:          { summary: "Indexed loop.", description: "Iterates over a range. Useful both in processes and in `generate` blocks.", example: "for i in 0 to 15 loop … end loop;" },
            while:        { summary: "Condition-based loop.", description: "Rarely synthesisable when the condition depends on signals." },
            next:         { summary: "Skip to the next iteration of a loop.", description: "" },
            exit:         { summary: "Break out of a loop.", description: "" },
            return:       { summary: "Returns a value from a function or exits a procedure.", description: "" },
            wait:         { summary: "Suspends a process.", description: "`wait on <signals>` / `wait until <cond>` / `wait for <time>`. Synthesisable only in specific shapes (e.g. `wait until rising_edge(clk)`).", example: "wait until rising_edge(clk);" },
            null:         { summary: "No-op statement.", description: "Placeholder where a statement is required syntactically." },

            // --- Concurrent / generate ---
            begin:        { summary: "Start of a statement block.", description: "Opens the body of `architecture`, `process`, `function`, `procedure`, `generate`." },
            end:          { summary: "Closes a block.", description: "Pairs with `entity`, `architecture`, `process`, `if`, `case`, `loop`, etc." },
            generate:     { summary: "Creates hardware conditionally or in loops.", description: "Think of it as a compile-time for-loop or if-statement that emits replicated or conditional structure.", example: "gen_bits : for i in 0 to 7 generate\n  q(i) <= d(7 - i);\nend generate;" },
            block:        { summary: "Scoped concurrent statement region.", description: "Groups statements under a guard condition. Rarely used in modern code." },
            assert:       { summary: "Simulation-time check.", description: "Fires a `report` and stops (or not, depending on severity) if the condition is false. Not synthesisable.", example: "assert data'length = 8 report \"wrong width\" severity error;" },
            report:       { summary: "Emits a simulation message.", description: "Often paired with `assert`. Severity can be `note`, `warning`, `error`, `failure`." },
            severity:     { summary: "Severity level for `assert`/`report`.", description: "" },

            // --- Subprograms ---
            function:     { summary: "Pure subprogram returning a value.", description: "Computes a value from its arguments — no side effects on signals.", example: "function max(a, b : integer) return integer is\nbegin\n  if a > b then return a; else return b; end if;\nend function;" },
            procedure:    { summary: "Subprogram that can drive signals.", description: "Unlike a function, a procedure can have `out` / `inout` parameters." },
            impure:       { summary: "Marks a function that can depend on external state.", description: "Allows reading signals declared outside its scope." },
            pure:         { summary: "Marks a function that has no external dependencies.", description: "The default for `function` — rarely spelled out." },
            return:       { summary: "Returns a value from a function or exits a procedure.", description: "" },
            open:         { summary: "Leaves a port unconnected, or opens a file.", description: "In a port map: `some_port => open;`. For file I/O: `file F : text open read_mode is ...;`" },

            // --- Operators (keywords) ---
            and:          { summary: "Logical AND.", description: "Element-wise on std_logic_vector / bit_vector." },
            or:           { summary: "Logical OR.", description: "Element-wise on vectors." },
            not:          { summary: "Logical NOT.", description: "Unary negation." },
            nand:         { summary: "Logical NAND.", description: "" },
            nor:          { summary: "Logical NOR.", description: "" },
            xor:          { summary: "Logical XOR.", description: "" },
            xnor:         { summary: "Logical XNOR.", description: "" },
            mod:          { summary: "Modulo.", description: "Result has the sign of the divisor (unlike `rem`)." },
            rem:          { summary: "Remainder.", description: "Result has the sign of the dividend." },
            abs:          { summary: "Absolute value.", description: "" },
            sla:          { summary: "Shift left arithmetic.", description: "" },
            sra:          { summary: "Shift right arithmetic.", description: "" },
            sll:          { summary: "Shift left logical.", description: "" },
            srl:          { summary: "Shift right logical.", description: "" },
            rol:          { summary: "Rotate left.", description: "" },
            ror:          { summary: "Rotate right.", description: "" },

            // --- Miscellaneous ---
            is:           { summary: "Separator keyword.", description: "Appears in `entity X is`, `architecture Y of X is`, `type T is`, etc." },
            of:           { summary: "Separator keyword.", description: "Appears in `architecture A of X`, `array(…) of …`." },
            all:          { summary: "Wildcard.", description: "Used in `use x.y.all;` and in attribute / sensitivity-list contexts." },
            new:          { summary: "Allocates an access-type (pointer) object.", description: "Rarely synthesisable." },
            label:        { summary: "Tags a statement with a name.", description: "Useful for processes, generates, and loops so you can refer to them later." },
            alias:        { summary: "Gives an alternative name to an object or part of it.", description: "", example: "alias sign : std_logic is data(7);" },
            attribute:    { summary: "Metadata tag or a predefined query.", description: "Queries like `x'length`, `x'range`, `signal'event`." },
            group:        { summary: "Groups named items for attribute application.", description: "Rarely seen." },
            bus:          { summary: "Obsolete kind-of-signal modifier.", description: "Historic." },
            register:     { summary: "Obsolete kind-of-signal modifier.", description: "Historic." },
            disconnect:   { summary: "Controls disconnection time on guarded signals.", description: "Historic / rare." },
            after:        { summary: "Transport delay specifier.", description: "`a <= b after 5 ns;` — a simulation-only delay." },
            reject:       { summary: "Inertial-delay rejection pulse.", description: "" },
            inertial:     { summary: "Inertial delay (default).", description: "Filters short pulses." },
            transport:    { summary: "Transport (non-filtering) delay.", description: "" },
            guarded:      { summary: "Qualifies a block or signal assignment.", description: "Rare." },
            linkage:      { summary: "Port direction for mixed-language linking.", description: "Only relevant in mixed VHDL/Verilog flows." },
            postponed:    { summary: "Delays a process or assertion until the end of the delta cycle.", description: "Simulation-only." },
            literal:      { summary: "Attribute-related keyword.", description: "Rare." },
            unaffected:   { summary: "'Do nothing' placeholder in a conditional signal assignment.", description: "" },
            until:        { summary: "Used with `wait until <condition>`.", description: "" },
            with:         { summary: "Starts a selected signal assignment.", description: "`with expr select …`." },
            on:           { summary: "Used in `wait on <signal-list>`.", description: "" }
        },

        types: {
            std_logic:        { summary: "IEEE 9-valued logic: one wire.", description: "Values: `'0'`, `'1'`, `'Z'` (high-Z), `'X'` (unknown), `'U'` (uninitialised), `'W'`, `'L'`, `'H'`, `'-'` (don't-care). The default for one-bit signals in synthesis.", example: "signal clk : std_logic := '0';" },
            std_logic_vector: { summary: "Vector of std_logic — a bus.", description: "Declared with a range: `(high downto low)` is the standard convention. Good for wiring; for arithmetic convert to `unsigned`/`signed`.", example: "signal data : std_logic_vector(7 downto 0);" },
            std_ulogic:       { summary: "Unresolved version of std_logic.", description: "One-driver-only. Rarely used directly." },
            std_ulogic_vector:{ summary: "Unresolved vector of std_ulogic.", description: "" },
            bit:              { summary: "Simple 2-valued logic: `'0'` or `'1'`.", description: "Predates std_logic. Use std_logic in synthesis — bit can't represent Z, X, etc." },
            bit_vector:       { summary: "Vector of `bit`.", description: "" },
            boolean:          { summary: "Boolean type: `true` / `false`.", description: "Used in conditions; cannot be directly synthesised onto a wire (convert to std_logic)." },
            integer:          { summary: "Integer — at least 32-bit signed.", description: "Great for counters and indices. Constrain with a subrange (e.g. `integer range 0 to 255`) so synthesis knows how many bits to use." },
            natural:          { summary: "`integer range 0 to integer'high`.", description: "Non-negative integer." },
            positive:         { summary: "`integer range 1 to integer'high`.", description: "Strictly positive integer." },
            real:             { summary: "Floating-point type.", description: "Not synthesisable — simulation / constants only." },
            character:        { summary: "ASCII / ISO-8859-1 character.", description: "" },
            string:           { summary: "Array of character.", description: "Used mostly for `report` messages and file I/O." },
            time:             { summary: "Physical type representing time.", description: "Units: `fs`, `ps`, `ns`, `us`, `ms`, `sec`, `min`, `hr`. Simulation only." },
            signed:           { summary: "Numeric vector (signed) from ieee.numeric_std.", description: "Use for signed arithmetic. Convert from std_logic_vector via the `signed()` cast.", example: "signal x : signed(7 downto 0);" },
            unsigned:         { summary: "Numeric vector (unsigned) from ieee.numeric_std.", description: "Use for unsigned arithmetic. Cast via `unsigned()` / `std_logic_vector()`.", example: "signal cnt : unsigned(7 downto 0) := (others => '0');" },
            line:             { summary: "Access to a text line — for file I/O.", description: "Used with `readline` / `writeline` in test benches." },
            text:             { summary: "File type used by the textio package.", description: "" },
            severity_level:   { summary: "Severity level for `assert`/`report`.", description: "Values: `note`, `warning`, `error`, `failure`." },
            side:             { summary: "Textio helper type.", description: "" },
            width:            { summary: "Textio helper type.", description: "" }
        },

        builtins: {
            rising_edge:    { summary: "True on a 0→1 transition of a clock.", signature: "rising_edge(s : std_ulogic) return boolean", description: "Preferred over `clk'event and clk = '1'`. Works for both std_logic and std_ulogic.", example: "if rising_edge(clk) then q <= d; end if;" },
            falling_edge:   { summary: "True on a 1→0 transition.", signature: "falling_edge(s : std_ulogic) return boolean", description: "Counterpart to `rising_edge`." },
            to_integer:     { summary: "Convert signed/unsigned to integer.", signature: "to_integer(arg : unsigned) return natural", description: "From `ieee.numeric_std`. Common cast after arithmetic.", example: "variable n : integer := to_integer(unsigned(data));" },
            to_unsigned:    { summary: "Convert integer to unsigned vector.", signature: "to_unsigned(arg : natural; size : natural) return unsigned", description: "", example: "counter <= to_unsigned(42, 8);" },
            to_signed:      { summary: "Convert integer to signed vector.", signature: "to_signed(arg : integer; size : natural) return signed", description: "" },
            resize:         { summary: "Resize a signed/unsigned vector.", signature: "resize(arg : unsigned; new_size : natural) return unsigned", description: "Zero-extends unsigned and sign-extends signed." },
            shift_left:     { summary: "Logical shift left.", signature: "shift_left(arg : unsigned; count : natural) return unsigned", description: "Zero-fills the right side." },
            shift_right:    { summary: "Logical shift right for unsigned / arithmetic for signed.", signature: "shift_right(arg : unsigned; count : natural) return unsigned", description: "" },
            rotate_left:    { summary: "Circular left shift.", signature: "rotate_left(arg : unsigned; count : natural) return unsigned", description: "" },
            rotate_right:   { summary: "Circular right shift.", signature: "rotate_right(arg : unsigned; count : natural) return unsigned", description: "" },
            conv_integer:   { summary: "Legacy integer conversion.", signature: "conv_integer(arg : std_logic_vector) return integer", description: "From `ieee.std_logic_arith`. Prefer `to_integer` from numeric_std in new code." },
            conv_std_logic_vector: { summary: "Legacy vector conversion.", signature: "conv_std_logic_vector(arg : integer; size : integer) return std_logic_vector", description: "Prefer `std_logic_vector(to_unsigned(...))` in new code." },
            now:            { summary: "Current simulation time.", signature: "now : time", description: "Simulation only." },
            readline:       { summary: "Read a line from a textio file.", signature: "readline(file f : text; l : out line)", description: "Testbench I/O." },
            writeline:      { summary: "Write a line to a textio file.", signature: "writeline(file f : text; l : inout line)", description: "Testbench I/O." },
            endline:        { summary: "End-of-line check.", signature: "endline(l : inout line) return boolean", description: "Textio helper." },
            endfile:        { summary: "End-of-file check.", signature: "endfile(file f : text) return boolean", description: "" },
            write:          { summary: "Textio write primitive.", signature: "write(l : inout line; value : … )", description: "" },
            read:           { summary: "Textio read primitive.", signature: "read(l : inout line; value : out … )", description: "" }
        },

        snippets: {
            entity: {
                label: "entity (skeleton)",
                summary: "Insert a minimal entity declaration",
                insertText: "entity ${1:name} is\n\tport (\n\t\t${2:clk}   : in  std_logic;\n\t\t${3:reset} : in  std_logic;\n\t\t${4:q}     : out std_logic_vector(${5:7} downto 0)\n\t);\nend entity ${1:name};\n\narchitecture behavioral of ${1:name} is\nbegin\n\t$0\nend architecture behavioral;"
            },
            process_clocked: {
                label: "process (clocked, async reset)",
                summary: "Standard clocked process with asynchronous reset",
                insertText: "process(${1:clk}, ${2:reset})\nbegin\n\tif ${2:reset} = '1' then\n\t\t$3 <= (others => '0');\n\telsif rising_edge(${1:clk}) then\n\t\t$0\n\tend if;\nend process;"
            },
            process_combinational: {
                label: "process (combinational)",
                summary: "Combinational process from a sensitivity list",
                insertText: "process(${1:all})\nbegin\n\t$0\nend process;"
            },
            if_else: {
                label: "if … elsif … else",
                summary: "Three-way conditional",
                insertText: "if ${1:condition} then\n\t$2\nelsif ${3:other} then\n\t$4\nelse\n\t$5\nend if;"
            },
            case_stmt: {
                label: "case … when … end case",
                summary: "Case statement skeleton",
                insertText: "case ${1:expr} is\n\twhen ${2:value} => $3\n\twhen others    => $0\nend case;"
            },
            for_loop: {
                label: "for i in 0 to N-1 loop",
                summary: "Indexed loop",
                insertText: "for ${1:i} in 0 to ${2:N}-1 loop\n\t$0\nend loop;"
            },
            signal: {
                label: "signal declaration",
                summary: "Declare a signal",
                insertText: "signal ${1:name} : ${2:std_logic_vector}(${3:7} downto 0) := (others => '0');"
            },
            counter: {
                label: "counter template",
                summary: "Wrap-around counter",
                insertText: "signal cnt : unsigned(${1:7} downto 0) := (others => '0');\n-- …\nprocess(${2:clk}, ${3:reset})\nbegin\n\tif ${3:reset} = '1' then\n\t\tcnt <= (others => '0');\n\telsif rising_edge(${2:clk}) then\n\t\tcnt <= cnt + 1;\n\tend if;\nend process;\n${4:q} <= std_logic_vector(cnt);"
            },
            shift_register: {
                label: "shift-register template",
                summary: "N-bit shift register",
                insertText: "signal sr : std_logic_vector(${1:7} downto 0) := (others => '0');\n-- …\nprocess(${2:clk})\nbegin\n\tif rising_edge(${2:clk}) then\n\t\tsr <= sr(${1:7}-1 downto 0) & ${3:d};\n\tend if;\nend process;\n${4:q} <= sr;"
            },
            library_ieee: {
                label: "library ieee + use clauses",
                summary: "Standard IEEE imports",
                insertText: "library ieee;\nuse ieee.std_logic_1164.all;\nuse ieee.numeric_std.all;\n\n$0"
            }
        },

        ui: {
            declaredOnLine: "declared on line",
            kindSignal:     "signal",
            kindVariable:   "variable",
            kindConstant:   "constant",
            kindPort:       "port",
            categoryKeyword:  "VHDL keyword",
            categoryType:     "Type",
            categoryBuiltin:  "IEEE function",
            categoryUser:     "User declaration",
            categorySnippet:  "Snippet",
            translationPending: "⚠ Translation pending — description shown in English."
        }
    };
})();
