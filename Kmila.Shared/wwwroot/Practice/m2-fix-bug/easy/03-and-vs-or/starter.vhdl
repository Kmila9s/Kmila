library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity and_vs_or is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity and_vs_or;

architecture behavioral of and_vs_or is
begin
    y <= a or b;
end architecture behavioral;
