library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity nor_fill is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity nor_fill;

architecture behavioral of nor_fill is
begin
    y <= a nor b;
end architecture behavioral;
