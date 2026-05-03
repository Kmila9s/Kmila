library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity and_fill is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity and_fill;

architecture behavioral of and_fill is
begin
    y <= a and b;
end architecture behavioral;
