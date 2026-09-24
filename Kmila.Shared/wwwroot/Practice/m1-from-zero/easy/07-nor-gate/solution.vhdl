library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity nor_gate is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity nor_gate;

architecture behavioral of nor_gate is
begin
    y <= a nor b;
end architecture behavioral;
