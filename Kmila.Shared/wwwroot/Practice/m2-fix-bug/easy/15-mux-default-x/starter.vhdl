library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity mux_def_x is
    port (
        a   : in  STD_LOGIC;
        b   : in  STD_LOGIC;
        sel : in  STD_LOGIC;
        y   : out STD_LOGIC
    );
end entity mux_def_x;

architecture behavioral of mux_def_x is
begin
    y <= a when sel = '0' else '1';
end architecture behavioral;
