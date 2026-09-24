library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity and3_buggy is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        c : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity and3_buggy;

architecture behavioral of and3_buggy is
begin
    y <= a and b and c;
end architecture behavioral;
