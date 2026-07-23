-- ============================================================================
-- clock_divider.vhd — parametric clock divider producing a single-cycle
-- strobe at 1/DIVISOR of the input frequency.
--
-- Kmila full audit 2026-07-08, Phase G, Ring 1.
--
-- Used by:
--   • keypad_scanner  — row scan tick every ~1 ms
--   • lcd_driver      — refresh tick every ~10 ms
--   • led_marquee     — animation tick every ~50 ms
--   • buzzer_ctrl     — PWM square-wave for audible tone
--
-- Note: this is a strobe (single-cycle pulse), not a divided clock. Sinks
--       clock off the fast domain and use `tick` as an enable — that keeps
--       the design single-clock-domain and avoids FPGA CDC gotchas.
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;

entity clock_divider is
    generic (
        DIVISOR : positive := 100_000    -- 100 MHz → 1 kHz default
    );
    port (
        clk    : in  std_logic;
        rst_n  : in  std_logic;          -- active-low async reset
        tick   : out std_logic            -- 1 clk cycle high every DIVISOR clocks
    );
end entity clock_divider;

architecture rtl of clock_divider is
    signal counter : unsigned(31 downto 0) := (others => '0');
    signal tick_r  : std_logic := '0';
begin

    process (clk, rst_n)
    begin
        if rst_n = '0' then
            counter <= (others => '0');
            tick_r  <= '0';
        elsif rising_edge(clk) then
            if counter = to_unsigned(DIVISOR - 1, counter'length) then
                counter <= (others => '0');
                tick_r  <= '1';
            else
                counter <= counter + 1;
                tick_r  <= '0';
            end if;
        end if;
    end process;

    tick <= tick_r;

end architecture rtl;
