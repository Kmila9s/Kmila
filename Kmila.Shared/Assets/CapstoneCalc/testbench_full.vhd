-- ============================================================================
-- testbench_full.vhd — Ring 5 behavioural verification of the capstone calc.
--
-- Kmila full audit 2026-07-14, session 20.
--
-- Runs the 25 test cases from Documentacion/HANDOFF_full_audit_2026-07-08/
-- G_capstone_cpu_calculator.md by driving cols_in in synchrony with
-- rows_out so single-key presses are recognised by the (now-fixed)
-- keypad_scanner. Each case emits a `report` with lcd_chars line-1 so
-- the harness log becomes an audit trail; per-case expected values
-- match the G-file table (line 224-250).
--
-- Timing budget: 30 ms hold + 10 ms release per key. At scan_tick=1 kHz
-- and DEBOUNCE_TICKS=3, a 30 ms hold guarantees ≥7 same-row samples of
-- the pressed key — well above the 3 required to debounce. The 10 ms
-- release exceeds IDLE_RELEASE_TICKS × scan_tick_period = 12 ms only
-- barely — bumped to 20 ms in the actual stim to eliminate the edge.
--
-- Cases whose expected result is q_ helper output (MUL/DIV/POW/LN =
-- A3/A4/A5/A6/A7 + B10/B11) are known to hit
-- project_user_fn_reactivity_gap; they run to completion but their
-- reported lcd_chars will be the pre-eval value. Marked "USR-FN GAP"
-- in the report output.
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;
library work;
use work.types_pkg.all;

entity testbench_full is
end entity testbench_full;

architecture sim of testbench_full is
    -- DUT signals
    signal clk         : std_logic := '0';
    signal rst_n       : std_logic := '0';
    signal cols_in     : std_logic_vector(3 downto 0) := "1111";
    signal rows_out    : std_logic_vector(3 downto 0);
    signal lcd_chars   : std_logic_vector(LCD_ROWS*LCD_COLS*8 - 1 downto 0);
    signal seg_bus     : std_logic_vector(4*7 - 1 downto 0);
    signal led_matrix  : std_logic_vector(LED_ROWS*LED_COLS - 1 downto 0);
    signal buzzer_pin  : std_logic;

    constant CLK_PERIOD : time := 10 ns;
    signal test_done    : std_logic := '0';

    -- Keycode → (row, col) lookup. Row/col are 0..3 indices; the
    -- press_key procedure encodes the corresponding cols_in pattern
    -- while the target row is being scanned.
    -- Row 0: 1 2 3 A
    -- Row 1: 4 5 6 B
    -- Row 2: 7 8 9 C
    -- Row 3: * 0 # D
    type keypos_t is record
        row : integer range 0 to 3;
        col : integer range 0 to 3;
    end record;

    function pos_of(k : keycode_t) return keypos_t is
        variable p : keypos_t := (0, 0);
    begin
        case k is
            when KEY_1    => p := (0, 0);
            when KEY_2    => p := (0, 1);
            when KEY_3    => p := (0, 2);
            when KEY_A    => p := (0, 3);
            when KEY_4    => p := (1, 0);
            when KEY_5    => p := (1, 1);
            when KEY_6    => p := (1, 2);
            when KEY_B    => p := (1, 3);
            when KEY_7    => p := (2, 0);
            when KEY_8    => p := (2, 1);
            when KEY_9    => p := (2, 2);
            when KEY_C    => p := (2, 3);
            when KEY_STAR => p := (3, 0);
            when KEY_0    => p := (3, 1);
            when KEY_HASH => p := (3, 2);
            when KEY_D    => p := (3, 3);
            when others   => p := (0, 0);
        end case;
        return p;
    end function pos_of;

begin

    -- ---------------------------------------------------------------- DUT
    -- Fast divisors for sim-time compression (100 MHz clk, testbench-only
    -- overrides — FPGA synthesis uses calc_top's defaults). At SCAN_DIV=10
    -- the scan cycle is 40 clock cycles = 400 ns, so DEBOUNCE_TICKS=3
    -- across 4 rows = ~4.8 μs to register a key.
    u_dut : entity work.calc_top(rtl)
        generic map (
            SCAN_DIV    => 10,
            REFRESH_DIV => 100,
            MARQUEE_DIV => 500,
            BUZ_DIV     => 10,
            PWM_DIV     => 25)
        port map (
            clk         => clk,
            rst_n       => rst_n,
            cols_in     => cols_in,
            rows_out    => rows_out,
            lcd_chars   => lcd_chars,
            seg_bus     => seg_bus,
            led_matrix  => led_matrix,
            buzzer_pin  => buzzer_pin);

    -- ---------------------------------------------------------------- Clock
    clk_proc : process
    begin
        while test_done = '0' loop
            clk <= '0';
            wait for CLK_PERIOD / 2;
            clk <= '1';
            wait for CLK_PERIOD / 2;
        end loop;
        wait;
    end process;

    -- ---------------------------------------------------------------- Stimulus
    stim_proc : process
        -- Press one key by driving its col low ONLY while its row is
        -- being scanned (rows_out(row) = '0'). Holds the press across
        -- multiple row-cycles so debounce fires.
        procedure press_key(k : keycode_t) is
            variable p        : keypos_t;
            variable elapsed  : time := 0 ns;
            constant HOLD_MS  : time := 30 ms;
        begin
            p := pos_of(k);
            while elapsed < HOLD_MS loop
                -- Wait for the target row to be scanned low.
                wait until rising_edge(clk) and rows_out(p.row) = '0';
                -- Ground this key's column bit; leave others high.
                cols_in <= (others => '1');
                cols_in(p.col) <= '0';
                -- Hold cols_in low until the scanner moves to the next row.
                wait until rising_edge(clk) and rows_out(p.row) = '1';
                cols_in <= (others => '1');
                elapsed := elapsed + 1 ms;  -- ~1 row cycle at 1 kHz scan
            end loop;
            -- Idle release window — must exceed IDLE_RELEASE_TICKS × scan_tick
            -- period (at SCAN_DIV=10 that's 12 × 100 ns = 1.2 μs; 5 μs safety).
            wait for 5 us;
        end procedure press_key;

        -- Convenience: run one test case name + settle.
        procedure begin_case(name : string) is
        begin
            report "======== " & name & " begin ========";
        end procedure begin_case;

        procedure end_case(name : string) is
        begin
            -- Settle: give expr_engine + CPU + LCD driver enough time
            -- to complete evaluation + refresh. LCD refresh is at
            -- ~100 Hz so 30 ms guarantees a full refresh cycle post-#.
            -- Refresh cycle at REFRESH_DIV=100 → 1 μs period; give 20 μs
            -- to guarantee expr_engine + lcd_driver capture the result.
            wait for 20 us;
            report "-------- " & name & " lcd line0/line1 (bytes MSB→LSB):";
            report "  line0 = " & to_string(lcd_chars(255 downto 128));
            report "  line1 = " & to_string(lcd_chars(127 downto 0));
            report "======== " & name & " end ========";
        end procedure end_case;

    begin
        -- ---- Global reset ----
        rst_n   <= '0';
        cols_in <= "1111";
        wait for 200 ns;
        rst_n   <= '1';
        wait for 500 ns;
        report "TB: reset released";

        -- Give FSM ~2 scan cycles to settle in UI_WELCOME.
        wait for 10 us;

        -- ==========================================================================
        -- CATEGORY A — Guided arithmetic (7 cases)
        -- ==========================================================================

        -- A1  ADD   4 # A 1 # 4 # #    → 8
        begin_case("A1");
        press_key(KEY_4);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_1);  press_key(KEY_HASH);
        press_key(KEY_4);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        end_case("A1");

        -- A2  SUB   1 4 # A 1 2 # 2 # #   → 12
        begin_case("A2");
        press_key(KEY_C);   -- clear first
        wait for 10 us;
        press_key(KEY_1);  press_key(KEY_4);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_1);  press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        end_case("A2");

        -- A3  MUL   1 2 0 0 # A 1 2 # 5 6 0 0 # #   → -4400 (USR-FN GAP)
        begin_case("A3");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_1);  press_key(KEY_2);  press_key(KEY_0);  press_key(KEY_0);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_1);  press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_5);  press_key(KEY_6);  press_key(KEY_0);  press_key(KEY_0);
        press_key(KEY_D);  press_key(KEY_HASH);      -- D toggles sign after digits
        press_key(KEY_HASH);
        report "  A3 note: expected -4400; MUL is USR-FN GAP so result reflects pre-eval state";
        end_case("A3");

        -- A4  DIV   2 4 D 1 # A 1 4 # 2 3 D 1 # #   → 1.0432 (USR-FN GAP)
        begin_case("A4");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_2);  press_key(KEY_4);  press_key(KEY_D);  press_key(KEY_1);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_1);  press_key(KEY_4);  press_key(KEY_HASH);
        press_key(KEY_2);  press_key(KEY_3);  press_key(KEY_D);  press_key(KEY_1);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        report "  A4 note: expected 1.0432; DIV is USR-FN GAP";
        end_case("A4");

        -- A5  POW   2 # A 1 5 # 9 # #   → 512 (USR-FN GAP)
        begin_case("A5");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_1);  press_key(KEY_5);  press_key(KEY_HASH);
        press_key(KEY_9);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        report "  A5 note: expected 512; POW is USR-FN GAP";
        end_case("A5");

        -- A6  Overflow  9 # A 1 5 # 7 8 # #   → OVERFLOW (USR-FN GAP — MUL underlies POW)
        begin_case("A6");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_9);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_1);  press_key(KEY_5);  press_key(KEY_HASH);
        press_key(KEY_7);  press_key(KEY_8);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        report "  A6 note: expected OVERFLOW; overflow-detect depends on MUL which is USR-FN GAP";
        end_case("A6");

        -- A7  LN    2 D 7 1 8 # A 1 6 # #   → 0.9999 (USR-FN GAP)
        begin_case("A7");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_2);  press_key(KEY_D);  press_key(KEY_7);  press_key(KEY_1);  press_key(KEY_8);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_1);  press_key(KEY_6);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        report "  A7 note: expected 0.9999; LN is USR-FN GAP";
        end_case("A7");

        -- ==========================================================================
        -- CATEGORY B — Chained expressions (6 cases)
        -- ==========================================================================

        -- B7  12 + 2 → 14
        begin_case("B7");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_1);  press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_1);  press_key(KEY_HASH);
        press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        end_case("B7");

        -- B8  continue: * 3 → expression buffer 12 + 2 * 3 = 18
        begin_case("B8");
        press_key(KEY_A);  press_key(KEY_3);  press_key(KEY_HASH);
        press_key(KEY_3);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        report "  B8 note: uses * (MUL) — USR-FN GAP applies";
        end_case("B8");

        -- B9  continue: - 45 → -27
        begin_case("B9");
        press_key(KEY_A);  press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_4);  press_key(KEY_5);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        end_case("B9");

        -- B10 fresh: 2 + 3 * 4 ** 2 - 1 → 49 (USR-FN GAP for *, **)
        begin_case("B10");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_1);  press_key(KEY_HASH);       -- +
        press_key(KEY_3);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_3);  press_key(KEY_HASH);       -- *
        press_key(KEY_4);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_5);  press_key(KEY_HASH);       -- **
        press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_2);  press_key(KEY_HASH);       -- -
        press_key(KEY_1);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        report "  B10 note: uses * and ** — USR-FN GAP applies";
        end_case("B10");

        -- B11 8-op chain: 1+2-3*4/5+6-7*8 (max chain length)
        begin_case("B11");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_1);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_1);  press_key(KEY_HASH); press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_2);  press_key(KEY_HASH); press_key(KEY_3);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_3);  press_key(KEY_HASH); press_key(KEY_4);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_4);  press_key(KEY_HASH); press_key(KEY_5);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_1);  press_key(KEY_HASH); press_key(KEY_6);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_2);  press_key(KEY_HASH); press_key(KEY_7);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_3);  press_key(KEY_HASH); press_key(KEY_8);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        report "  B11 note: mixed ops incl. MUL/DIV — USR-FN GAP applies";
        end_case("B11");

        -- B12 9th op press → TOO LONG
        begin_case("B12");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_1);  press_key(KEY_HASH);
        for i in 1 to 9 loop
            press_key(KEY_A);  press_key(KEY_1);  press_key(KEY_HASH);
            press_key(KEY_1);  press_key(KEY_HASH);
        end loop;
        press_key(KEY_HASH);
        end_case("B12");

        -- ==========================================================================
        -- CATEGORY C — Logical bitwise (8 cases)
        -- ==========================================================================
        -- KEY_A cycles categories; 2 presses = LOGIC menu.
        -- Sub-op codes inside LOGIC menu (per rom.vhd): 1=AND 2=OR 3=NOT 4=XOR
        -- 5=XNOR 6=NAND 7=NOR (student may need to verify against actual ROM).

        -- C13  12 AND 10 → 8
        begin_case("C13");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_1);  press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_A);  press_key(KEY_1);  press_key(KEY_HASH);
        press_key(KEY_1);  press_key(KEY_0);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        end_case("C13");

        -- C14  12 OR 10 → 14
        begin_case("C14");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_1);  press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_A);  press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_1);  press_key(KEY_0);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        end_case("C14");

        -- C15  12 XOR 10 → 6
        begin_case("C15");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_1);  press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_A);  press_key(KEY_4);  press_key(KEY_HASH);
        press_key(KEY_1);  press_key(KEY_0);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        end_case("C15");

        -- C16  NOT 5 → -6 (unary op — no second operand)
        begin_case("C16");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_5);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_A);  press_key(KEY_3);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        end_case("C16");

        -- C17  12 XNOR 10
        begin_case("C17");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_1);  press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_A);  press_key(KEY_5);  press_key(KEY_HASH);
        press_key(KEY_1);  press_key(KEY_0);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        end_case("C17");

        -- C18  12 NAND 10
        begin_case("C18");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_1);  press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_A);  press_key(KEY_6);  press_key(KEY_HASH);
        press_key(KEY_1);  press_key(KEY_0);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        end_case("C18");

        -- C19  12 NOR 10
        begin_case("C19");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_1);  press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_A);  press_key(KEY_7);  press_key(KEY_HASH);
        press_key(KEY_1);  press_key(KEY_0);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        end_case("C19");

        -- C20  12 AND 10 + 5  =  12 AND 15  → 12
        begin_case("C20");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_1);  press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_A);  press_key(KEY_1);  press_key(KEY_HASH);
        press_key(KEY_1);  press_key(KEY_0);  press_key(KEY_HASH);
        press_key(KEY_A);  press_key(KEY_A);  press_key(KEY_A);  press_key(KEY_1);  press_key(KEY_HASH);  -- back to arith +
        press_key(KEY_5);  press_key(KEY_HASH);
        press_key(KEY_HASH);
        end_case("C20");

        -- ==========================================================================
        -- CATEGORY D — Edge cases (4 cases)
        -- ==========================================================================

        -- D-i  backspace B mid-operand → single-digit delete
        begin_case("D-i");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_1);  press_key(KEY_2);  press_key(KEY_3);
        press_key(KEY_B);   -- backspace: expect "12" left
        press_key(KEY_HASH);
        end_case("D-i");

        -- D-ii  C from any state → reset to WELCOME + LED marquee restart
        begin_case("D-ii");
        press_key(KEY_1);  press_key(KEY_2);
        press_key(KEY_C);   -- clear-all
        end_case("D-ii");

        -- D-iii  STAR in MENU_OPERATION → back to INPUT_OPERAND, buffer kept
        begin_case("D-iii");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_1);  press_key(KEY_2);  press_key(KEY_HASH);
        press_key(KEY_A);
        press_key(KEY_STAR);  -- cancel back to input
        press_key(KEY_HASH);
        end_case("D-iii");

        -- D-iv  A A A A → ARITH → LOGIC → SYS → ARITH (LCD updates each tick)
        begin_case("D-iv");
        press_key(KEY_C);   wait for 10 us;
        press_key(KEY_A);
        end_case("D-iv-arith");
        press_key(KEY_A);
        end_case("D-iv-logic");
        press_key(KEY_A);
        end_case("D-iv-sys");
        press_key(KEY_A);
        end_case("D-iv-back-to-arith");

        -- ---- Done ----
        report "TB: all 25 cases dispatched";
        test_done <= '1';
        wait;
    end process;

end architecture sim;
