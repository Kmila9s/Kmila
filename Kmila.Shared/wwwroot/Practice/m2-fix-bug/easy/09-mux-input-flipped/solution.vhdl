library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity mux_flip is
    port (
        a   : in  STD_LOGIC;
        b   : in  STD_LOGIC;
        sel : in  STD_LOGIC;
        y   : out STD_LOGIC
    );
end entity mux_flip;

architecture behavioral of mux_flip is
begin
    y <= a when sel = '0' else b;
end architecture behavioral;
