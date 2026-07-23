-- ============================================================================
-- led_marquee.vhd — 3×3 LED matrix sequencer for LedMatrixWidget.
--
-- Kmila full audit 2026-07-08, Phase G, Ring 1.
--
-- Patterns (selected by `mode`):
--   • MODE_WELCOME  = "00"  → column scroll left-to-right ("waking" animation)
--   • MODE_HEALTH   = "01"  → single-pixel heartbeat in the center
--   • MODE_ERROR    = "10"  → full-matrix strobe (all-on / all-off alternating)
--
-- Output is a 9-bit flat vector (row-major, [8]=top-left … [0]=bottom-right)
-- to match how LedMatrixWidget's bindings are read from a std_logic_vector
-- port in Kmila. If Kmila's Visual bindings prefer a 2-D `std_logic_matrix`,
-- flag that mismatch as a P2 finding — do NOT reshape the VHDL to fit.
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;
library work;
use work.types_pkg.all;

entity led_marquee is
    port (
        clk       : in  std_logic;
        rst_n     : in  std_logic;
        tick      : in  std_logic;                    -- animation strobe (~20 Hz)
        mode      : in  std_logic_vector(1 downto 0); -- 00=welcome 01=health 10=error
        led_out   : out std_logic_vector(LED_ROWS*LED_COLS - 1 downto 0)
    );
end entity led_marquee;

architecture rtl of led_marquee is
    constant MODE_WELCOME : std_logic_vector(1 downto 0) := "00";
    constant MODE_HEALTH  : std_logic_vector(1 downto 0) := "01";
    constant MODE_ERROR   : std_logic_vector(1 downto 0) := "10";

    signal frame : unsigned(3 downto 0) := (others => '0');   -- 0..15
    signal buf   : std_logic_vector(LED_ROWS*LED_COLS - 1 downto 0) := (others => '0');

    -- Row-major helpers so the pattern definitions stay readable. Bit index
    -- for (row, col) is (row * LED_COLS) + col.
    function px(r, c : natural) return natural is
    begin
        return r * LED_COLS + c;
    end function;

begin

    process (clk, rst_n)
        variable next_frame : unsigned(3 downto 0);
        variable next_buf   : std_logic_vector(LED_ROWS*LED_COLS - 1 downto 0);
    begin
        if rst_n = '0' then
            frame <= (others => '0');
            buf   <= (others => '0');
        elsif rising_edge(clk) then
            if tick = '1' then
                next_frame := frame + 1;
                next_buf   := (others => '0');

                case mode is
                    when MODE_WELCOME =>
                        -- Column scroll: light column (frame mod 3) top-to-bottom.
                        for r in 0 to LED_ROWS - 1 loop
                            next_buf(px(r, to_integer(next_frame) mod LED_COLS)) := '1';
                        end loop;

                    when MODE_HEALTH =>
                        -- Heartbeat: center pixel on frames 0..3, off 4..15
                        if next_frame < to_unsigned(4, 4) then
                            next_buf(px(1, 1)) := '1';
                        end if;

                    when MODE_ERROR =>
                        -- Alternating full-on / full-off — bit 0 of frame.
                        if next_frame(0) = '1' then
                            next_buf := (others => '1');
                        else
                            next_buf := (others => '0');
                        end if;

                    when others =>
                        next_buf := (others => '0');
                end case;

                frame <= next_frame;
                buf   <= next_buf;
            end if;
        end if;
    end process;

    led_out <= buf;

end architecture rtl;
