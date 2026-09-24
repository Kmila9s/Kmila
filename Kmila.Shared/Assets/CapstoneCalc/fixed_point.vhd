-- ============================================================================
-- fixed_point.vhd — Q16.16 arithmetic helpers for the capstone ALU.
--
-- Kmila full audit 2026-07-08, Phase G, Ring 2.
--
-- Q16.16 representation:
--   • 32-bit signed word.
--   • Bits [31:16] = integer part (signed two's-complement).
--   • Bits [15: 0] = fractional part (unsigned magnitude of the fraction).
--   • So `Q16_ONE = 2**16` and each ULP represents 1/65536.
--
-- Watched risks per memory (project_user_fn_reactivity_gap):
--   User-defined functions called from inside a process may freeze at their
--   first-eval value in Kmila. The ALU consumes these as `signal := q_mul(a,b)`
--   in a *combinational* process, which should re-evaluate every delta.
--   If Kmila still freezes, that's a P0/P1 finding — do NOT rewrite.
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;
library work;
use work.types_pkg.all;

package fixed_point is

    -- Multiply two Q16.16 values. Uses a 64-bit signed intermediate.
    function q_mul(a, b : q16_t) return q16_t;

    -- Divide a by b (Q16.16). Returns Q16_ZERO on b = 0 (caller sets DIV0 flag).
    function q_div(a, b : q16_t) return q16_t;

    -- Truncate a Q16.16 to its integer part (still Q16.16, fraction = 0).
    function q_trunc(a : q16_t) return q16_t;

    -- Absolute value.
    function q_abs(a : q16_t) return q16_t;

    -- Integer power: a**n where n is a small unsigned exponent (0..15).
    -- Uses square-and-multiply. n > 15 saturates at a**15.
    function q_pow(a : q16_t; n : unsigned(3 downto 0)) return q16_t;

    -- Natural logarithm approximated with a 16-entry LUT for ln(1)..ln(2)
    -- + linear interpolation. Domain: a > 0. Returns Q16_ZERO for a <= 0.
    -- Range-reduces via powers of 2: ln(a) = ln(a / 2**k) + k * ln(2).
    function q_ln(a : q16_t) return q16_t;

end package fixed_point;

package body fixed_point is

    -- 16-entry LUT for ln(1), ln(1 + 1/16), ..., ln(2 - 1/16), all in Q16.16.
    -- Values computed as `round(ln(1 + i/16) * 65536)` for i in 0..15.
    type ln_lut_t is array (0 to 15) of q16_t;
    constant LN_LUT : ln_lut_t := (
        to_signed(     0, WORD_W),   -- ln(1.0000) = 0.00000
        to_signed(  3974, WORD_W),   -- ln(1.0625) ≈ 0.06062
        to_signed(  7719, WORD_W),   -- ln(1.1250) ≈ 0.11778
        to_signed( 11263, WORD_W),   -- ln(1.1875) ≈ 0.17185
        to_signed( 14624, WORD_W),   -- ln(1.2500) ≈ 0.22314
        to_signed( 17822, WORD_W),   -- ln(1.3125) ≈ 0.27193
        to_signed( 20873, WORD_W),   -- ln(1.3750) ≈ 0.31845
        to_signed( 23791, WORD_W),   -- ln(1.4375) ≈ 0.36291
        to_signed( 26589, WORD_W),   -- ln(1.5000) ≈ 0.40546
        to_signed( 29277, WORD_W),   -- ln(1.5625) ≈ 0.44629
        to_signed( 31865, WORD_W),   -- ln(1.6250) ≈ 0.48551
        to_signed( 34362, WORD_W),   -- ln(1.6875) ≈ 0.52325
        to_signed( 36774, WORD_W),   -- ln(1.7500) ≈ 0.55962
        to_signed( 39107, WORD_W),   -- ln(1.8125) ≈ 0.59471
        to_signed( 41369, WORD_W),   -- ln(1.8750) ≈ 0.62861
        to_signed( 43563, WORD_W)    -- ln(1.9375) ≈ 0.66139
    );

    -- ln(2) in Q16.16 ≈ 0.69315 * 65536 = 45426.
    constant Q_LN2 : q16_t := to_signed(45426, WORD_W);

    -- --------------------------------------------------------------------
    function q_mul(a, b : q16_t) return q16_t is
        variable prod64 : signed(2*WORD_W - 1 downto 0);
    begin
        prod64 := a * b;                                    -- 64-bit signed
        return resize(shift_right(prod64, Q_FRAC), WORD_W); -- back to Q16.16
    end function q_mul;

    -- --------------------------------------------------------------------
    function q_div(a, b : q16_t) return q16_t is
        variable num64 : signed(2*WORD_W - 1 downto 0);
    begin
        if b = Q16_ZERO then
            return Q16_ZERO;
        end if;
        -- Shift a left by Q_FRAC so the quotient stays in Q16.16.
        num64 := resize(a, 2*WORD_W) sll Q_FRAC;
        return resize(num64 / resize(b, 2*WORD_W), WORD_W);
    end function q_div;

    -- --------------------------------------------------------------------
    function q_trunc(a : q16_t) return q16_t is
        variable int_part : signed(WORD_W-1 downto 0);
    begin
        -- Zero out the low 16 fractional bits.
        int_part := shift_right(a, Q_FRAC);
        return shift_left(int_part, Q_FRAC);
    end function q_trunc;

    -- --------------------------------------------------------------------
    function q_abs(a : q16_t) return q16_t is
    begin
        if a < Q16_ZERO then
            return -a;
        else
            return a;
        end if;
    end function q_abs;

    -- --------------------------------------------------------------------
    function q_pow(a : q16_t; n : unsigned(3 downto 0)) return q16_t is
        variable acc : q16_t := Q16_ONE;
        variable base : q16_t := a;
        variable exp : unsigned(3 downto 0) := n;
    begin
        -- Square-and-multiply: iterate through the 4 bits of n.
        for i in 0 to 3 loop
            if exp(0) = '1' then
                acc := q_mul(acc, base);
            end if;
            base := q_mul(base, base);
            exp := shift_right(exp, 1);
        end loop;
        return acc;
    end function q_pow;

    -- --------------------------------------------------------------------
    function q_ln(a : q16_t) return q16_t is
        variable v      : q16_t;
        variable k      : integer;
        variable idx    : integer;
        variable result : q16_t;
    begin
        if a <= Q16_ZERO then
            return Q16_ZERO;   -- undefined; caller should set error
        end if;

        -- Range reduction: bring v into [1.0, 2.0) via k left/right shifts.
        v := a;
        k := 0;
        while v >= shift_left(Q16_ONE, 1) loop     -- v >= 2.0
            v := shift_right(v, 1);
            k := k + 1;
        end loop;
        while v < Q16_ONE loop                     -- v < 1.0
            v := shift_left(v, 1);
            k := k - 1;
        end loop;

        -- Now v is in [Q16_ONE, 2*Q16_ONE). Extract the top 4 fraction bits
        -- as the LUT index (0..15).
        idx := to_integer(unsigned(std_logic_vector(v(Q_FRAC-1 downto Q_FRAC-4))));

        -- Interpolate: base = LN_LUT(idx), delta = frac-below-lut-tick * slope.
        -- Slope between LUT entries is ~1/16 * ln'(1+idx/16). For a first
        -- pass we skip interpolation and just take the LUT entry — worst-case
        -- error ~1/32 = 3%. Ring-3+ can add linear correction if needed.
        result := LN_LUT(idx) + resize(to_signed(k, WORD_W) * Q_LN2, WORD_W);
        return result;
    end function q_ln;

end package body fixed_point;
