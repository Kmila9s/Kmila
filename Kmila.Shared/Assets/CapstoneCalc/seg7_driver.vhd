-- ============================================================================
-- seg7_driver.vhd — 4-digit 7-segment display driver for SevenSegmentWidget.
--
-- Kmila full audit 2026-07-08, Phase G, Ring 2.
--
-- Takes a 16-bit unsigned value (0..9999) and drives 4× 7-segment digits.
-- Blank leading zeros so "42" reads as "  42" not "0042".
--
-- The Kmila SevenSegmentWidget takes 4 concatenated 7-bit segment vectors
-- ({seg_d3, seg_d2, seg_d1, seg_d0}, each abcdefg). This driver is purely
-- combinational — refresh multiplexing is the widget's responsibility.
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;
library work;
use work.types_pkg.all;

entity seg7_driver is
    port (
        value    : in  unsigned(13 downto 0);   -- 0..16383 covers 0..9999 + slack
        blank    : in  std_logic;                -- when '1', all digits blank
        seg_bus  : out std_logic_vector(4*7 - 1 downto 0)
    );
end entity seg7_driver;

architecture rtl of seg7_driver is
    -- 7-seg encoding, active-high, order a-b-c-d-e-f-g (bit 6 = a).
    -- Digit 0..9 + blank(0=all off, appears as sentinel index 10).
    type seg_lut_t is array (0 to 10) of std_logic_vector(6 downto 0);
    constant SEG_LUT : seg_lut_t := (
        0  => "1111110",   -- 0
        1  => "0110000",   -- 1
        2  => "1101101",   -- 2
        3  => "1111001",   -- 3
        4  => "0110011",   -- 4
        5  => "1011011",   -- 5
        6  => "1011111",   -- 6
        7  => "1110000",   -- 7
        8  => "1111111",   -- 8
        9  => "1111011",   -- 9
        10 => "0000000"    -- blank
    );

    -- Decompose value into 4 BCD digits by successive divide-by-10.
    signal d0, d1, d2, d3 : integer range 0 to 10 := 10;
begin

    process (value, blank)
        variable v    : integer;
        variable dd0  : integer range 0 to 9;
        variable dd1  : integer range 0 to 9;
        variable dd2  : integer range 0 to 9;
        variable dd3  : integer range 0 to 9;
    begin
        if blank = '1' then
            d0 <= 10;
            d1 <= 10;
            d2 <= 10;
            d3 <= 10;
        else
            v   := to_integer(value);
            dd0 := v mod 10;
            dd1 := (v / 10) mod 10;
            dd2 := (v / 100) mod 10;
            dd3 := (v / 1000) mod 10;

            d0 <= dd0;
            -- Leading-zero blanking: blank d1 if higher digits AND dd1 are all 0.
            if dd1 = 0 and dd2 = 0 and dd3 = 0 then
                d1 <= 10;
            else
                d1 <= dd1;
            end if;
            if dd2 = 0 and dd3 = 0 then
                d2 <= 10;
            else
                d2 <= dd2;
            end if;
            if dd3 = 0 then
                d3 <= 10;
            else
                d3 <= dd3;
            end if;
        end if;
    end process;

    seg_bus <= SEG_LUT(d3) & SEG_LUT(d2) & SEG_LUT(d1) & SEG_LUT(d0);

end architecture rtl;
