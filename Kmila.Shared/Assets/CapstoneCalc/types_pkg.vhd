-- ============================================================================
-- types_pkg.vhd — shared constants + types for the capstone 32-bit CPU calculator
--
-- Kmila full audit 2026-07-08, Phase G, Ring 1.
-- Pure package: no processes, no ports. Every other file `use`s this.
--
-- If Kmila's LibraryCompiler stubs out any of numeric_std, this file is
-- where that shows up first — flag as P1 per the "Kmila adapts to VHDL"
-- rule and do NOT rewrite to work around it.
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;

package types_pkg is
    -- ==========================================================================
    -- Word widths
    -- ==========================================================================
    constant WORD_W    : natural := 32;   -- CPU word / ALU width / Q16.16 total
    constant Q_FRAC    : natural := 16;   -- fractional bits in Q16.16
    constant Q_INT     : natural := 16;   -- integer bits in Q16.16 (signed)

    subtype word_t is signed(WORD_W-1 downto 0);
    subtype q16_t  is signed(WORD_W-1 downto 0);   -- Q16.16 fixed-point
    subtype byte_t is std_logic_vector(7 downto 0);

    -- Handy constants for the expression engine + display drivers.
    constant Q16_ONE     : q16_t := to_signed(2**Q_FRAC, WORD_W);
    constant Q16_ZERO    : q16_t := (others => '0');
    constant Q16_NEG_ONE : q16_t := to_signed(-1 * 2**Q_FRAC, WORD_W);

    -- ==========================================================================
    -- Opcode encoding — 5 bits (fits arithmetic + logical + system ops)
    -- ==========================================================================
    subtype opcode_t is std_logic_vector(4 downto 0);

    -- Arithmetic
    constant OP_ADD   : opcode_t := "00001";
    constant OP_SUB   : opcode_t := "00010";
    constant OP_MUL   : opcode_t := "00011";
    constant OP_DIV   : opcode_t := "00100";
    constant OP_POW   : opcode_t := "00101";
    constant OP_LN    : opcode_t := "00110";

    -- Logical (bitwise on the raw Q16.16 representation)
    constant OP_AND   : opcode_t := "01000";
    constant OP_OR    : opcode_t := "01001";
    constant OP_NOT   : opcode_t := "01010";   -- unary
    constant OP_XOR   : opcode_t := "01011";
    constant OP_XNOR  : opcode_t := "01100";
    constant OP_NAND  : opcode_t := "01101";
    constant OP_NOR   : opcode_t := "01110";

    -- Sentinel — used by expr_engine when peeking at end of token queue.
    constant OP_NONE  : opcode_t := "00000";

    -- ==========================================================================
    -- UI mode FSM — 3 bits fits the 5 modes with headroom
    -- ==========================================================================
    subtype ui_mode_t is std_logic_vector(2 downto 0);

    constant UI_WELCOME       : ui_mode_t := "000";
    constant UI_INPUT_OPERAND : ui_mode_t := "001";
    constant UI_MENU_OP       : ui_mode_t := "010";
    constant UI_EVALUATE      : ui_mode_t := "011";
    constant UI_ERROR         : ui_mode_t := "100";

    -- ==========================================================================
    -- Error codes surfaced to the LCD + buzzer + marquee
    -- ==========================================================================
    subtype err_code_t is std_logic_vector(1 downto 0);
    constant ERR_NONE     : err_code_t := "00";
    constant ERR_OVERFLOW : err_code_t := "01";
    constant ERR_DIV0     : err_code_t := "10";
    constant ERR_TOOLONG  : err_code_t := "11";

    -- ==========================================================================
    -- Expression-engine limits
    -- ==========================================================================
    constant MAX_OPS      : natural := 8;              -- 9 operands
    constant MAX_OPERANDS : natural := MAX_OPS + 1;

    -- ==========================================================================
    -- Keypad encoding — 4-bit keycode + 1-bit strobe
    -- ==========================================================================
    subtype keycode_t is std_logic_vector(3 downto 0);
    constant KEY_0     : keycode_t := "0000";
    constant KEY_1     : keycode_t := "0001";
    constant KEY_2     : keycode_t := "0010";
    constant KEY_3     : keycode_t := "0011";
    constant KEY_4     : keycode_t := "0100";
    constant KEY_5     : keycode_t := "0101";
    constant KEY_6     : keycode_t := "0110";
    constant KEY_7     : keycode_t := "0111";
    constant KEY_8     : keycode_t := "1000";
    constant KEY_9     : keycode_t := "1001";
    constant KEY_A     : keycode_t := "1010";  -- menu category cycle
    constant KEY_B     : keycode_t := "1011";  -- backspace
    constant KEY_C     : keycode_t := "1100";  -- clear all
    constant KEY_D     : keycode_t := "1101";  -- sign / decimal
    constant KEY_STAR  : keycode_t := "1110";  -- cancel / back
    constant KEY_HASH  : keycode_t := "1111";  -- enter / evaluate

    -- ==========================================================================
    -- Display widths
    -- ==========================================================================
    constant LCD_COLS  : natural := 16;
    constant LCD_ROWS  : natural := 2;
    constant SEG7_DIGITS : natural := 4;
    constant LED_ROWS  : natural := 3;
    constant LED_COLS  : natural := 3;

    -- ==========================================================================
    -- Buzzer tone durations (in the buzzer_ctrl's tick domain, ~1 kHz)
    -- ==========================================================================
    constant BUZ_SHORT_TICKS  : natural := 20;    -- key click
    constant BUZ_MEDIUM_TICKS : natural := 120;   -- operation committed
    constant BUZ_LONG_TICKS   : natural := 800;   -- error

end package types_pkg;

package body types_pkg is
    -- No function bodies here; all constants are declared inline.
    -- Q16.16 helpers live in fixed_point.vhd so this package stays
    -- import-cheap and Kmila's package resolver only has to walk
    -- constants, not process code.
end package body types_pkg;
