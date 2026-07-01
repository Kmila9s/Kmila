-- Testbench skeleton for {{PROJECT_IDENT}}.
-- The Kmila simulator runs this directly; stimulate from the Ports tab,
-- or extend the process below with extra waveforms.

library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity {{PROJECT_IDENT}}_tb is
end entity {{PROJECT_IDENT}}_tb;

architecture sim of {{PROJECT_IDENT}}_tb is
  signal clk   : STD_LOGIC := '0';
  signal reset : STD_LOGIC := '1';
  signal a_in  : STD_LOGIC_VECTOR(7 downto 0) := (others => '0');
  signal b_in  : STD_LOGIC_VECTOR(7 downto 0) := (others => '0');
  signal sum   : STD_LOGIC_VECTOR(8 downto 0);
begin

  -- Instantiate the design under test.
  dut: entity work.{{PROJECT_IDENT}}
    port map (
      clk   => clk,
      reset => reset,
      a_in  => a_in,
      b_in  => b_in,
      sum   => sum
    );

  -- 50 MHz clock — 20 ns period.
  clk <= not clk after 10 ns;

  -- Stimulus: release reset, sweep a few input pairs.
  stim: process
  begin
    wait for 30 ns;
    reset <= '0';

    a_in <= x"01"; b_in <= x"02";
    wait for 40 ns;
    a_in <= x"10"; b_in <= x"20";
    wait for 40 ns;
    a_in <= x"FF"; b_in <= x"01";
    wait for 40 ns;

    wait;
  end process;
end architecture sim;
