library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity inv_buggy is
    port (
        a : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity inv_buggy;

architecture behavioral of inv_buggy is
begin
    y <= a;
end architecture behavioral;
