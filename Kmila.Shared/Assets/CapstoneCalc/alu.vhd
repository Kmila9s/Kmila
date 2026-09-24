-- ============================================================================
-- alu.vhd — 32-bit combinational ALU for the capstone calculator.
--
-- Kmila full audit 2026-07-08, Phase G, Ring 2.
--
-- Wraps fixed_point.vhd for arithmetic and applies straightforward bitwise
-- ops for the logic side. Combinational block; the CPU registers the result
-- on its clock edge.
--
-- Flags:
--   ovf   = overflow (add/sub carry mismatch, mul truncates, pow saturates)
--   zero  = result is Q16_ZERO
--   neg   = MSB of result set (signed negative)
--   div0  = OP_DIV with b = 0  (caller must set UI to error)
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;
library work;
use work.types_pkg.all;
use work.fixed_point.all;

entity alu is
    port (
        a       : in  q16_t;
        b       : in  q16_t;
        op      : in  opcode_t;
        result  : out q16_t;
        ovf     : out std_logic;
        zero    : out std_logic;
        neg     : out std_logic;
        div0    : out std_logic
    );
end entity alu;

architecture rtl of alu is
    signal r_int  : q16_t := Q16_ZERO;
    signal ovf_i  : std_logic := '0';
    signal div0_i : std_logic := '0';

    -- Overflow detection for ADD: sign(a) = sign(b) and sign(r) /= sign(a).
    function add_ovf(a, b, r : q16_t) return std_logic is
    begin
        if (a(WORD_W-1) = b(WORD_W-1)) and (r(WORD_W-1) /= a(WORD_W-1)) then
            return '1';
        else
            return '0';
        end if;
    end function;

    -- Overflow for SUB: same as ADD(a, -b, r).
    function sub_ovf(a, b, r : q16_t) return std_logic is
    begin
        if (a(WORD_W-1) /= b(WORD_W-1)) and (r(WORD_W-1) /= a(WORD_W-1)) then
            return '1';
        else
            return '0';
        end if;
    end function;

begin

    -- Combinational selector — every op maps to a Q16.16 signed operation.
    process (a, b, op)
        variable sum : q16_t;
        variable dif : q16_t;
        variable pw  : q16_t;
    begin
        ovf_i  <= '0';
        div0_i <= '0';

        case op is
            when OP_ADD =>
                sum := a + b;
                r_int <= sum;
                ovf_i <= add_ovf(a, b, sum);

            when OP_SUB =>
                dif := a - b;
                r_int <= dif;
                ovf_i <= sub_ovf(a, b, dif);

            when OP_MUL =>
                r_int <= q_mul(a, b);

            when OP_DIV =>
                if b = Q16_ZERO then
                    r_int  <= Q16_ZERO;
                    div0_i <= '1';
                else
                    r_int <= q_div(a, b);
                end if;

            when OP_POW =>
                -- b's low 4 bits are the exponent (0..15).
                pw := q_pow(a, unsigned(std_logic_vector(b(3 downto 0))));
                r_int <= pw;

            when OP_LN =>
                r_int <= q_ln(a);

            when OP_AND  => r_int <= a and b;
            when OP_OR   => r_int <= a or b;
            when OP_NOT  => r_int <= not a;
            when OP_XOR  => r_int <= a xor b;
            when OP_XNOR => r_int <= a xnor b;
            when OP_NAND => r_int <= a nand b;
            when OP_NOR  => r_int <= a nor b;

            when others =>
                r_int <= Q16_ZERO;
        end case;
    end process;

    result <= r_int;
    ovf    <= ovf_i;
    div0   <= div0_i;
    zero   <= '1' when r_int = Q16_ZERO else '0';
    neg    <= r_int(WORD_W - 1);

end architecture rtl;
