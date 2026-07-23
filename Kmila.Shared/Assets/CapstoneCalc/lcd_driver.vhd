-- ============================================================================
-- lcd_driver.vhd — 16×2 character LCD driver for LcdWidget.
--
-- Kmila full audit 2026-07-08, Phase G, Ring 2.
--
-- Consumes two 16-character lines (bytes packed MSB-first) and emits them
-- as a 256-bit flat vector for LcdWidget's `chars` binding, plus a `refresh`
-- pulse whenever the content changes and a `line_dirty` bitmask so a smart
-- widget could redraw only the changed row.
--
-- Not attempting scroll here — expressions longer than 16 chars are
-- pre-windowed by expr_engine before writing to `line0`.
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;
library work;
use work.types_pkg.all;

entity lcd_driver is
    port (
        clk       : in  std_logic;
        rst_n     : in  std_logic;
        refresh   : in  std_logic;                            -- ~100 Hz tick
        line0     : in  std_logic_vector(LCD_COLS*8 - 1 downto 0);
        line1     : in  std_logic_vector(LCD_COLS*8 - 1 downto 0);
        chars_out : out std_logic_vector(LCD_ROWS*LCD_COLS*8 - 1 downto 0);
        line_dirty: out std_logic_vector(LCD_ROWS - 1 downto 0)
    );
end entity lcd_driver;

architecture rtl of lcd_driver is
    signal l0_reg     : std_logic_vector(LCD_COLS*8 - 1 downto 0) := (others => '0');
    signal l1_reg     : std_logic_vector(LCD_COLS*8 - 1 downto 0) := (others => '0');
    signal dirty_reg  : std_logic_vector(LCD_ROWS - 1 downto 0) := (others => '0');
begin

    process (clk, rst_n)
    begin
        if rst_n = '0' then
            l0_reg    <= (others => '0');
            l1_reg    <= (others => '0');
            dirty_reg <= (others => '0');
        elsif rising_edge(clk) then
            if refresh = '1' then
                dirty_reg <= (others => '0');
                if line0 /= l0_reg then
                    l0_reg <= line0;
                    dirty_reg(0) <= '1';
                end if;
                if line1 /= l1_reg then
                    l1_reg <= line1;
                    dirty_reg(1) <= '1';
                end if;
            end if;
        end if;
    end process;

    -- Row 1 is the LOW half of chars_out, row 0 is the HIGH half — this
    -- matches how LcdWidget renders bytes from MSB-to-LSB left-to-right
    -- top-to-bottom.
    chars_out  <= l0_reg & l1_reg;
    line_dirty <= dirty_reg;

end architecture rtl;
