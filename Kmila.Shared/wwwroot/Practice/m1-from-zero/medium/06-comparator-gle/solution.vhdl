library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity comparator_gle is
    port (
        a  : in  STD_LOGIC_VECTOR(3 downto 0);
        b  : in  STD_LOGIC_VECTOR(3 downto 0);
        gt : out STD_LOGIC;
        eq : out STD_LOGIC;
        lt : out STD_LOGIC
    );
end entity comparator_gle;

architecture behavioral of comparator_gle is
begin
    gt <= '1' when unsigned(a) >  unsigned(b) else '0';
    eq <= '1' when unsigned(a) =  unsigned(b) else '0';
    lt <= '1' when unsigned(a) <  unsigned(b) else '0';
end architecture behavioral;
