-- Testbench for {{PROJECT_IDENT}}'s FSM.
-- Exercises a single trigger pulse and waits long enough to observe the
-- IDLE → WORK → DONE → IDLE traversal.

library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity {{PROJECT_IDENT}}_tb is
end entity {{PROJECT_IDENT}}_tb;

architecture sim of {{PROJECT_IDENT}}_tb is
  signal clk     : STD_LOGIC := '0';
  signal reset   : STD_LOGIC := '1';
  signal trigger : STD_LOGIC := '0';
  signal done    : STD_LOGIC;
begin

  dut: entity work.{{PROJECT_IDENT}}
    port map (
      clk     => clk,
      reset   => reset,
      trigger => trigger,
      done    => done
    );

  -- 50 MHz clock.
  clk <= not clk after 10 ns;

  stim: process
  begin
    wait for 25 ns;
    reset <= '0';

    -- One short trigger pulse.
    wait for 40 ns;
    trigger <= '1';
    wait for 20 ns;
    trigger <= '0';

    -- Idle long enough to watch the cycle complete.
    wait for 200 ns;

    wait;
  end process;
end architecture sim;
