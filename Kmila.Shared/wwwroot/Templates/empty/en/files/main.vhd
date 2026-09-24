-- Empty scaffold for {{PROJECT_NAME}}.
-- A minimal but synthesizable entity so the project builds and runs
-- immediately. Replace the body with whatever your design actually needs.

library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity {{PROJECT_IDENT}} is
  port (
    clk     : in  STD_LOGIC;
    reset   : in  STD_LOGIC;
    enable  : in  STD_LOGIC;
    out_bit : out STD_LOGIC
  );
end entity {{PROJECT_IDENT}};

architecture behavioral of {{PROJECT_IDENT}} is
  signal q : STD_LOGIC := '0';
begin
  process(clk, reset)
  begin
    if reset = '1' then
      q <= '0';
    elsif rising_edge(clk) then
      if enable = '1' then
        q <= not q;
      end if;
    end if;
  end process;

  out_bit <= q;
end architecture behavioral;
