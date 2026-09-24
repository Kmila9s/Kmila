library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity not_gate is
    port (
        a : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity not_gate;

architecture behavioral of not_gate is
begin
    y <= not a;
end architecture behavioral;
