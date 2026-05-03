library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity mux4to1 is
    port (
        a   : in  STD_LOGIC;
        b   : in  STD_LOGIC;
        c   : in  STD_LOGIC;
        d   : in  STD_LOGIC;
        sel : in  STD_LOGIC_VECTOR(1 downto 0);
        y   : out STD_LOGIC
    );
end entity mux4to1;

architecture behavioral of mux4to1 is
begin
    -- TODO: pick one of a, b, c, d based on sel.
    y <= '0';
end architecture behavioral;
