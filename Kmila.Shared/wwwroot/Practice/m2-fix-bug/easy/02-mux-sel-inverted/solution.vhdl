library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity mux_sel_inv is
    port (
        a   : in  STD_LOGIC;
        b   : in  STD_LOGIC;
        sel : in  STD_LOGIC;
        y   : out STD_LOGIC
    );
end entity mux_sel_inv;

architecture behavioral of mux_sel_inv is
begin
    y <= a when sel = '0' else b;
end architecture behavioral;
