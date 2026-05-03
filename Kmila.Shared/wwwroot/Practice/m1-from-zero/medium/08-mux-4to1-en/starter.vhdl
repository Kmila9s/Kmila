library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity mux4to1_en is
    port (
        a   : in  STD_LOGIC;
        b   : in  STD_LOGIC;
        c   : in  STD_LOGIC;
        d   : in  STD_LOGIC;
        sel : in  STD_LOGIC_VECTOR(1 downto 0);
        en  : in  STD_LOGIC;
        y   : out STD_LOGIC
    );
end entity mux4to1_en;

architecture behavioral of mux4to1_en is
begin
    -- TODO: y is 0 when en=0, otherwise route a/b/c/d based on sel.
    y <= '0';
end architecture behavioral;
