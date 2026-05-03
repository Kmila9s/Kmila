library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity mux2to1 is
    port (
        a   : in  STD_LOGIC;
        b   : in  STD_LOGIC;
        sel : in  STD_LOGIC;
        y   : out STD_LOGIC
    );
end entity mux2to1;

architecture behavioral of mux2to1 is
begin
    -- TODO: route a or b to y based on sel.
    y <= '0';
end architecture behavioral;
