-- Testbench for {{PROJECT_IDENT}}.
-- Drives a 4-bit instance long enough to observe one full wrap + carry.

library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity {{PROJECT_IDENT}}_tb is
end entity {{PROJECT_IDENT}}_tb;

architecture sim of {{PROJECT_IDENT}}_tb is
  signal clk    : STD_LOGIC := '0';
  signal reset  : STD_LOGIC := '1';
  signal enable : STD_LOGIC := '0';
  signal count4 : STD_LOGIC_VECTOR(3 downto 0);
  signal carry4 : STD_LOGIC;
begin

  -- 4-bit instance keeps the wrap visible within a short sim window.
  dut: entity work.{{PROJECT_IDENT}}
    generic map (
      WIDTH => 4
    )
    port map (
      clk    => clk,
      reset  => reset,
      enable => enable,
      count  => count4,
      carry  => carry4
    );

  -- 50 MHz clock.
  clk <= not clk after 10 ns;

  stim: process
  begin
    wait for 25 ns;
    reset  <= '0';

    enable <= '1';
    -- 20 clocks @ 20 ns = 400 ns, more than one full 4-bit wrap (16 ticks).
    wait for 400 ns;

    -- Park and observe stop behaviour.
    enable <= '0';
    wait for 100 ns;

    wait;
  end process;
end architecture sim;
