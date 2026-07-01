-- {{PROJECT_NAME}} — top-level design for the assignment.
-- Fill in the ports + architecture according to the spec, then drive
-- the inputs from tb/main_tb.vhd.

library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity {{PROJECT_IDENT}} is
  port (
    clk    : in  STD_LOGIC;
    reset  : in  STD_LOGIC;
    -- TODO: replace these with the ports your assignment requires.
    a_in   : in  STD_LOGIC_VECTOR(7 downto 0);
    b_in   : in  STD_LOGIC_VECTOR(7 downto 0);
    sum    : out STD_LOGIC_VECTOR(8 downto 0)
  );
end entity {{PROJECT_IDENT}};

architecture rtl of {{PROJECT_IDENT}} is
  signal acc : unsigned(8 downto 0) := (others => '0');
begin
  process(clk, reset)
  begin
    if reset = '1' then
      acc <= (others => '0');
    elsif rising_edge(clk) then
      -- TODO: replace this placeholder with your assignment logic.
      acc <= resize(unsigned(a_in), 9) + resize(unsigned(b_in), 9);
    end if;
  end process;

  sum <= STD_LOGIC_VECTOR(acc);
end architecture rtl;
