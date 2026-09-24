-- ============================================================================
-- cpu.vhd — top-level FSM driving the capstone calculator.
--
-- Kmila full audit 2026-07-08, Phase G, Ring 3.
--
-- Not a general-purpose CPU — it's a UI-driven controller with a small
-- register file that orchestrates:
--   • Keypad → digit accumulator (INPUT_OPERAND mode).
--   • KEY_A → operator category cycle (MENU_OP mode).
--   • KEY_HASH → commit expression + trigger expr_engine (EVALUATE mode).
--   • KEY_C  → reset UI + registers (any mode → WELCOME).
--   • Buzzer + marquee drivers on each state transition.
--
-- Register file: 16× 32-bit q16_t. Layout:
--   r0        = zero (constant)
--   r1        = current operand digit accumulator
--   r2        = last committed operand
--   r3        = last result
--   r4..r7    = scratch
--   r8..r15   = operand history (for expr_engine to read as needed)
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;
library work;
use work.types_pkg.all;

entity cpu is
    port (
        clk           : in  std_logic;
        rst_n         : in  std_logic;
        tick          : in  std_logic;           -- ~1 kHz strobe

        -- Keypad in
        key_strobe    : in  std_logic;
        keycode       : in  keycode_t;

        -- Expr_engine handshake
        eng_start     : out std_logic;
        eng_done      : in  std_logic;
        eng_busy      : in  std_logic;
        eng_result    : in  q16_t;
        eng_err       : in  err_code_t;

        -- UI outputs
        ui_mode       : out ui_mode_t;
        cur_operand   : out q16_t;
        last_result   : out q16_t;
        marquee_mode  : out std_logic_vector(1 downto 0);
        buz_click     : out std_logic;
        buz_op        : out std_logic;
        buz_error     : out std_logic
    );
end entity cpu;

architecture rtl of cpu is
    type regfile_t is array (0 to 15) of q16_t;
    signal regs : regfile_t := (others => Q16_ZERO);

    signal mode_reg    : ui_mode_t := UI_WELCOME;
    signal start_reg   : std_logic := '0';
    signal click_reg   : std_logic := '0';
    signal op_reg      : std_logic := '0';
    signal err_pulse   : std_logic := '0';

    -- Accumulate digits into r1: r1 <= r1 * 10 + key_digit.
    function digit_of(k : keycode_t) return q16_t is
    begin
        case k is
            when KEY_0 => return to_signed(0, WORD_W);
            when KEY_1 => return to_signed(1 * 2**Q_FRAC, WORD_W);
            when KEY_2 => return to_signed(2 * 2**Q_FRAC, WORD_W);
            when KEY_3 => return to_signed(3 * 2**Q_FRAC, WORD_W);
            when KEY_4 => return to_signed(4 * 2**Q_FRAC, WORD_W);
            when KEY_5 => return to_signed(5 * 2**Q_FRAC, WORD_W);
            when KEY_6 => return to_signed(6 * 2**Q_FRAC, WORD_W);
            when KEY_7 => return to_signed(7 * 2**Q_FRAC, WORD_W);
            when KEY_8 => return to_signed(8 * 2**Q_FRAC, WORD_W);
            when KEY_9 => return to_signed(9 * 2**Q_FRAC, WORD_W);
            when others => return Q16_ZERO;
        end case;
    end function;

    function is_digit(k : keycode_t) return boolean is
    begin
        return k = KEY_0 or k = KEY_1 or k = KEY_2 or k = KEY_3 or k = KEY_4
            or k = KEY_5 or k = KEY_6 or k = KEY_7 or k = KEY_8 or k = KEY_9;
    end function;

    -- r1 * 10 = r1 shifted-left(3) + r1 shifted-left(1). Cheaper than a
    -- full multiplier when we only need integer ×10 during digit entry.
    function times_ten(x : q16_t) return q16_t is
    begin
        return shift_left(x, 3) + shift_left(x, 1);
    end function;
begin

    process (clk, rst_n)
    begin
        if rst_n = '0' then
            regs      <= (others => Q16_ZERO);
            mode_reg  <= UI_WELCOME;
            start_reg <= '0';
            click_reg <= '0';
            op_reg    <= '0';
            err_pulse <= '0';
        elsif rising_edge(clk) then
            -- Default: pulses fall to 0 unless re-asserted below.
            start_reg <= '0';
            click_reg <= '0';
            op_reg    <= '0';
            err_pulse <= '0';

            if key_strobe = '1' then
                click_reg <= '1';   -- every key gets a click

                case mode_reg is
                    when UI_WELCOME =>
                        -- Any digit starts input.
                        if is_digit(keycode) then
                            regs(1)  <= digit_of(keycode);
                            mode_reg <= UI_INPUT_OPERAND;
                        elsif keycode = KEY_C then
                            regs     <= (others => Q16_ZERO);
                        end if;

                    when UI_INPUT_OPERAND =>
                        if is_digit(keycode) then
                            regs(1) <= times_ten(regs(1)) + digit_of(keycode);
                        elsif keycode = KEY_B then
                            -- Backspace: divide by 10 (integer).
                            regs(1) <= shift_right(regs(1), 3) + shift_right(regs(1), 1);
                        elsif keycode = KEY_A then
                            -- Commit current operand, enter operator menu.
                            regs(2)  <= regs(1);
                            mode_reg <= UI_MENU_OP;
                            op_reg   <= '1';
                        elsif keycode = KEY_HASH then
                            -- Evaluate current expression.
                            mode_reg  <= UI_EVALUATE;
                            start_reg <= '1';
                            op_reg    <= '1';
                        elsif keycode = KEY_C then
                            regs     <= (others => Q16_ZERO);
                            mode_reg <= UI_WELCOME;
                        end if;

                    when UI_MENU_OP =>
                        -- Selecting an operator key returns to operand input.
                        if is_digit(keycode) then
                            regs(1)  <= digit_of(keycode);
                            mode_reg <= UI_INPUT_OPERAND;
                        elsif keycode = KEY_STAR then
                            mode_reg <= UI_INPUT_OPERAND;   -- cancel menu
                        elsif keycode = KEY_C then
                            regs     <= (others => Q16_ZERO);
                            mode_reg <= UI_WELCOME;
                        end if;

                    when UI_EVALUATE =>
                        if keycode = KEY_C then
                            regs     <= (others => Q16_ZERO);
                            mode_reg <= UI_WELCOME;
                        end if;

                    when UI_ERROR =>
                        if keycode = KEY_C then
                            regs     <= (others => Q16_ZERO);
                            mode_reg <= UI_WELCOME;
                        end if;

                    when others =>
                        mode_reg <= UI_WELCOME;
                end case;
            end if;

            -- Watch expr_engine handshake.
            if mode_reg = UI_EVALUATE then
                if eng_done = '1' then
                    if eng_err /= ERR_NONE then
                        mode_reg  <= UI_ERROR;
                        err_pulse <= '1';
                    else
                        regs(3)   <= eng_result;
                        mode_reg  <= UI_INPUT_OPERAND;
                        op_reg    <= '1';
                    end if;
                end if;
            end if;
        end if;
    end process;

    ui_mode      <= mode_reg;
    cur_operand  <= regs(1);
    last_result  <= regs(3);

    -- Marquee mode: welcome scroll while UI_WELCOME, health heartbeat
    -- otherwise, error strobe on UI_ERROR. Matches led_marquee.vhd's
    -- 00/01/10 encoding.
    marquee_mode <= "00" when mode_reg = UI_WELCOME
              else "10" when mode_reg = UI_ERROR
              else "01";

    eng_start    <= start_reg;
    buz_click    <= click_reg;
    buz_op       <= op_reg;
    buz_error    <= err_pulse;

end architecture rtl;
