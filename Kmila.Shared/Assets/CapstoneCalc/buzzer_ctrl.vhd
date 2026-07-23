-- ============================================================================
-- buzzer_ctrl.vhd — three-mode audio feedback driver for BuzzerWidget.
--
-- Kmila full audit 2026-07-08, Phase G, Ring 1.
--
-- Modes (triggered by a rising edge on the corresponding input):
--   • click_pulse    → short burst (BUZ_SHORT_TICKS  · buzzer tick)  ≈ 20 ms
--   • op_pulse       → medium tone (BUZ_MEDIUM_TICKS · buzzer tick) ≈ 120 ms
--   • error_pulse    → long alarm  (BUZ_LONG_TICKS   · buzzer tick) ≈ 800 ms
--
-- Output `buzzer` is a squarewave at ~2 kHz (audible) gated by the countdown.
-- Silent when idle.
--
-- Clock domain: same clk as the rest of the design. `buz_tick` from the
-- clock_divider (typically 1 kHz) drives the countdown; `buzzer` toggles on
-- every `pwm_tick` (typically 4 kHz) so the audible pitch is ~2 kHz square.
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;
library work;
use work.types_pkg.all;

entity buzzer_ctrl is
    port (
        clk         : in  std_logic;
        rst_n       : in  std_logic;
        buz_tick    : in  std_logic;   -- countdown tick (~1 kHz)
        pwm_tick    : in  std_logic;   -- square-wave toggle tick (~4 kHz)
        click_pulse : in  std_logic;   -- rising edge → SHORT
        op_pulse    : in  std_logic;   -- rising edge → MEDIUM
        error_pulse : in  std_logic;   -- rising edge → LONG
        buzzer      : out std_logic
    );
end entity buzzer_ctrl;

architecture rtl of buzzer_ctrl is
    signal countdown : integer range 0 to BUZ_LONG_TICKS := 0;
    signal square    : std_logic := '0';

    -- Rising-edge detectors on the three pulse inputs.
    signal click_r, op_r, err_r : std_logic := '0';
begin

    process (clk, rst_n)
    begin
        if rst_n = '0' then
            countdown <= 0;
            square    <= '0';
            click_r   <= '0';
            op_r      <= '0';
            err_r     <= '0';
        elsif rising_edge(clk) then
            -- Store previous values of the pulse inputs so we can detect
            -- rising edges. This avoids re-triggering on a held-high pulse.
            click_r <= click_pulse;
            op_r    <= op_pulse;
            err_r   <= error_pulse;

            -- Priority: error > op > click. If two events land on the same
            -- clock, the more severe one wins.
            if (error_pulse = '1' and err_r = '0') then
                countdown <= BUZ_LONG_TICKS;
            elsif (op_pulse = '1' and op_r = '0') then
                countdown <= BUZ_MEDIUM_TICKS;
            elsif (click_pulse = '1' and click_r = '0') then
                countdown <= BUZ_SHORT_TICKS;
            elsif buz_tick = '1' and countdown > 0 then
                countdown <= countdown - 1;
            end if;

            -- Square-wave PWM while countdown > 0.
            if pwm_tick = '1' and countdown > 0 then
                square <= not square;
            elsif countdown = 0 then
                square <= '0';
            end if;
        end if;
    end process;

    buzzer <= square;

end architecture rtl;
