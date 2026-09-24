-- ============================================================================
-- rom.vhd — microprogram + operator dispatch table for the capstone.
--
-- Kmila full audit 2026-07-08, Phase G, Ring 3.
--
-- Two lookup surfaces:
--   • keycode → opcode: when a category key (menu KEY_A cycle) selects an
--     operator, the input keypad code (KEY_1..KEY_9) maps to an arithmetic
--     or logical opcode. This mimics the physical calculator "press 1 for
--     ADD, 2 for SUB, ..." menu.
--   • opcode → precedence: for shunting-yard, each opcode has a 3-bit
--     precedence class (0 = lowest / bitwise; 7 = unary; MUL/DIV/POW higher
--     than ADD/SUB).
--
-- Both surfaces are combinational — the CPU/expr_engine read them
-- directly by index.
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;
library work;
use work.types_pkg.all;

entity rom is
    port (
        -- keycode → opcode lookup (input operator-menu key)
        key_in    : in  keycode_t;
        op_out    : out opcode_t;

        -- opcode → precedence lookup
        op_in     : in  opcode_t;
        prec_out  : out std_logic_vector(2 downto 0)
    );
end entity rom;

architecture rtl of rom is
    -- keycode → opcode selection table. Menu category ARITH (default):
    --   KEY_1=ADD  KEY_2=SUB  KEY_3=MUL  KEY_4=DIV  KEY_5=POW  KEY_6=LN
    -- Category LOGIC (after KEY_A cycle):
    --   Handled via a separate map upstream; ROM here only serves the ARITH
    --   default lane. Extending to categories requires an extra input port.
    function key_to_op(k : keycode_t) return opcode_t is
    begin
        case k is
            when KEY_1 => return OP_ADD;
            when KEY_2 => return OP_SUB;
            when KEY_3 => return OP_MUL;
            when KEY_4 => return OP_DIV;
            when KEY_5 => return OP_POW;
            when KEY_6 => return OP_LN;
            when others => return OP_NONE;
        end case;
    end function;

    -- Standard operator precedence.
    function op_to_prec(op : opcode_t) return std_logic_vector is
    begin
        case op is
            when OP_LN                    => return "111";  -- unary, highest
            when OP_NOT                   => return "111";  -- unary bitwise
            when OP_POW                   => return "110";
            when OP_MUL | OP_DIV          => return "101";
            when OP_ADD | OP_SUB          => return "100";
            when OP_AND | OP_NAND         => return "011";
            when OP_XOR | OP_XNOR         => return "010";
            when OP_OR  | OP_NOR          => return "001";
            when others                   => return "000";
        end case;
    end function;

begin

    op_out   <= key_to_op(key_in);
    prec_out <= op_to_prec(op_in);

end architecture rtl;
