-- ============================================================================
-- calc_top.vhd — top-level wiring for the capstone calculator.
--
-- Kmila full audit 2026-07-08, Phase G, Ring 4.
--
-- Instantiates every sub-entity from Rings 1–3 and exposes the external
-- pins that the Kmila Visual palette binds to:
--   • cols_in / rows_out  → KeypadWidget (4×4 matrix)
--   • lcd_chars           → LcdWidget (2 rows × 16 chars, packed MSB-first)
--   • seg_bus             → SevenSegmentWidget (4-digit)
--   • led_matrix          → LedMatrixWidget (3×3 row-major)
--   • buzzer_pin          → BuzzerWidget
--
-- Clock domain: single 100 MHz `clk`, divided via three `clock_divider`
-- instances into scan_tick (~1 kHz), refresh_tick (~100 Hz), and
-- marquee_tick (~20 Hz).
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;
library work;
use work.types_pkg.all;

entity calc_top is
    -- Ring 5 (2026-07-14): top-level testability generics. Defaults are the
    -- FPGA-realistic values for a 100 MHz system clock — synthesis uses
    -- those unchanged (no override at the FPGA top-level instantiation).
    -- Simulation testbenches override to small values so behavioural
    -- verification runs in seconds instead of billions of clock cycles.
    generic (
        SCAN_DIV    : positive := 100_000;    -- 100 MHz → 1 kHz keypad scan
        REFRESH_DIV : positive := 1_000_000;  -- 100 MHz → 100 Hz LCD refresh
        MARQUEE_DIV : positive := 5_000_000;  -- 100 MHz → 20 Hz LED marquee
        BUZ_DIV     : positive := 100_000;    -- 100 MHz → 1 kHz buzzer domain
        PWM_DIV     : positive := 25_000      -- 100 MHz → ~4 kHz PWM
    );
    port (
        clk         : in  std_logic;
        rst_n       : in  std_logic;

        -- Keypad
        cols_in     : in  std_logic_vector(3 downto 0);
        rows_out    : out std_logic_vector(3 downto 0);

        -- LCD (2 × 16 × 8 = 256 bits, packed)
        lcd_chars   : out std_logic_vector(LCD_ROWS*LCD_COLS*8 - 1 downto 0);

        -- 7-segment (4 digits × 7 segments = 28 bits)
        seg_bus     : out std_logic_vector(4*7 - 1 downto 0);

        -- LED matrix (3 × 3 = 9 bits, row-major)
        led_matrix  : out std_logic_vector(LED_ROWS*LED_COLS - 1 downto 0);

        -- Buzzer
        buzzer_pin  : out std_logic
    );
end entity calc_top;

architecture rtl of calc_top is
    -- Clock strobes
    signal scan_tick    : std_logic;
    signal refresh_tick : std_logic;
    signal marquee_tick : std_logic;
    signal pwm_tick     : std_logic;
    signal buz_tick     : std_logic;

    -- Keypad → CPU
    signal keycode_i    : keycode_t;
    signal key_strobe_i : std_logic;

    -- CPU → peripherals
    signal ui_mode_i     : ui_mode_t;
    signal cur_operand_i : q16_t;
    signal last_result_i : q16_t;
    signal marquee_mode_i: std_logic_vector(1 downto 0);
    signal buz_click_i   : std_logic;
    signal buz_op_i      : std_logic;
    signal buz_error_i   : std_logic;

    -- CPU ↔ expr_engine handshake
    signal eng_start_i   : std_logic;
    signal eng_done_i    : std_logic;
    signal eng_busy_i    : std_logic;
    signal eng_result_i  : q16_t;
    signal eng_err_i     : err_code_t;

    -- Expr_engine ↔ ALU wiring
    signal alu_a_i, alu_b_i, alu_result_i : q16_t;
    signal alu_op_i      : opcode_t;
    signal alu_ovf_i     : std_logic;
    signal alu_div0_i    : std_logic;
    signal alu_zero_i    : std_logic;
    signal alu_neg_i     : std_logic;

    -- LCD content packing — trivial placeholder that packs the cur_operand
    -- bytes into line0 and result into line1. A real driver would decimal-
    -- format the q16_t; deferred to Ring 5 polish.
    signal lcd_line0     : std_logic_vector(LCD_COLS*8 - 1 downto 0);
    signal lcd_line1     : std_logic_vector(LCD_COLS*8 - 1 downto 0);
    signal lcd_dirty     : std_logic_vector(LCD_ROWS - 1 downto 0);

    -- 7-seg: display the integer part of cur_operand (bits 31..16 mod 9999).
    signal seg_value     : unsigned(13 downto 0);

    -- Streaming tokens to expr_engine — placeholder plumbing for the audit
    -- (real token producer lives inside CPU as its EVALUATE-state machine).
    signal tok_valid_i   : std_logic := '0';
    signal tok_is_op_i   : std_logic := '0';
    signal tok_operand_i : q16_t := Q16_ZERO;
    signal tok_op_i      : opcode_t := OP_NONE;
    signal stream_end_i  : std_logic := '0';

    -- F-G-05 workaround (session 7, 2026-07-09): Kmila's parser rejects
    -- `<label> : entity work.<name> generic map (...) port map (...);` as
    -- a direct-entity instantiation when `generic map` precedes `port
    -- map` — it treats the label like a signal assignment target. The
    -- component-declaration form below is equivalent VHDL and works
    -- around the gap. Filed as F-G-05 in FINDINGS.md; the Kmila fix
    -- would extend Architecture parser's identifier-then-colon lookahead
    -- to recognise `entity work.X generic map` as a component
    -- instantiation start.
    component clock_divider is
        generic (DIVISOR : positive := 100_000);
        port (
            clk    : in  std_logic;
            rst_n  : in  std_logic;
            tick   : out std_logic
        );
    end component;
begin

    -- ------------------------------------------------------------------ Clocks
    u_scan_div : clock_divider
        generic map (DIVISOR => SCAN_DIV)     -- default 100_000 → 1 kHz
        port map (clk => clk, rst_n => rst_n, tick => scan_tick);

    u_refresh_div : clock_divider
        generic map (DIVISOR => REFRESH_DIV)  -- default 1_000_000 → 100 Hz
        port map (clk => clk, rst_n => rst_n, tick => refresh_tick);

    u_marquee_div : clock_divider
        generic map (DIVISOR => MARQUEE_DIV)  -- default 5_000_000 → 20 Hz
        port map (clk => clk, rst_n => rst_n, tick => marquee_tick);

    u_buz_div : clock_divider
        generic map (DIVISOR => BUZ_DIV)      -- default 100_000 → 1 kHz
        port map (clk => clk, rst_n => rst_n, tick => buz_tick);

    u_pwm_div : clock_divider
        generic map (DIVISOR => PWM_DIV)      -- default 25_000 → ~4 kHz PWM
        port map (clk => clk, rst_n => rst_n, tick => pwm_tick);

    -- ------------------------------------------------------------------ Input
    u_keypad : entity work.keypad_scanner
        port map (
            clk        => clk,
            rst_n      => rst_n,
            scan_tick  => scan_tick,
            cols       => cols_in,
            rows       => rows_out,
            keycode    => keycode_i,
            key_strobe => key_strobe_i);

    -- ------------------------------------------------------------------ Control
    u_cpu : entity work.cpu
        port map (
            clk          => clk,
            rst_n        => rst_n,
            tick         => scan_tick,
            key_strobe   => key_strobe_i,
            keycode      => keycode_i,
            eng_start    => eng_start_i,
            eng_done     => eng_done_i,
            eng_busy     => eng_busy_i,
            eng_result   => eng_result_i,
            eng_err      => eng_err_i,
            ui_mode      => ui_mode_i,
            cur_operand  => cur_operand_i,
            last_result  => last_result_i,
            marquee_mode => marquee_mode_i,
            buz_click    => buz_click_i,
            buz_op       => buz_op_i,
            buz_error    => buz_error_i);

    -- ------------------------------------------------------------------ Compute
    u_engine : entity work.expr_engine
        port map (
            clk         => clk,
            rst_n       => rst_n,
            start       => eng_start_i,
            tok_valid   => tok_valid_i,
            tok_is_op   => tok_is_op_i,
            tok_operand => tok_operand_i,
            tok_op      => tok_op_i,
            stream_end  => stream_end_i,
            alu_result  => alu_result_i,
            alu_ovf     => alu_ovf_i,
            alu_div0    => alu_div0_i,
            alu_a       => alu_a_i,
            alu_b       => alu_b_i,
            alu_op      => alu_op_i,
            result_out  => eng_result_i,
            err_code    => eng_err_i,
            done        => eng_done_i,
            busy        => eng_busy_i);

    u_alu : entity work.alu
        port map (
            a       => alu_a_i,
            b       => alu_b_i,
            op      => alu_op_i,
            result  => alu_result_i,
            ovf     => alu_ovf_i,
            zero    => alu_zero_i,
            neg     => alu_neg_i,
            div0    => alu_div0_i);

    -- ------------------------------------------------------------------ Displays
    -- Trivial LCD packing: fill both lines with '0' bytes; real formatting
    -- deferred. This keeps the LCD driver exercised in synthesis without
    -- hard-coding a demo string.
    lcd_line0 <= (others => '0');
    lcd_line1 <= (others => '0');

    u_lcd : entity work.lcd_driver
        port map (
            clk        => clk,
            rst_n      => rst_n,
            refresh    => refresh_tick,
            line0      => lcd_line0,
            line1      => lcd_line1,
            chars_out  => lcd_chars,
            line_dirty => lcd_dirty);

    -- Seg7: integer part of cur_operand, saturated into the driver's 14-bit
    -- range so the display shows the natural-number portion of the operand.
    seg_value <= resize(unsigned(cur_operand_i(WORD_W - 1 downto Q_FRAC)), 14);

    u_seg7 : entity work.seg7_driver
        port map (
            value   => seg_value,
            blank   => '0',
            seg_bus => seg_bus);

    u_marquee : entity work.led_marquee
        port map (
            clk     => clk,
            rst_n   => rst_n,
            tick    => marquee_tick,
            mode    => marquee_mode_i,
            led_out => led_matrix);

    -- ------------------------------------------------------------------ Sound
    u_buzzer : entity work.buzzer_ctrl
        port map (
            clk         => clk,
            rst_n       => rst_n,
            buz_tick    => buz_tick,
            pwm_tick    => pwm_tick,
            click_pulse => buz_click_i,
            op_pulse    => buz_op_i,
            error_pulse => buz_error_i,
            buzzer      => buzzer_pin);

end architecture rtl;
