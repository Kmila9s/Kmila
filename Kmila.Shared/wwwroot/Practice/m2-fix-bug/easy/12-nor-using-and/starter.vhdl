library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity nor_buggy is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity nor_buggy;

architecture behavioral of nor_buggy is
begin
    y <= a nor (not b);
end architecture behavioral;
