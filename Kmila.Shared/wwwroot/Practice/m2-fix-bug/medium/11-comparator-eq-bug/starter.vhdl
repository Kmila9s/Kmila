library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity cmp_eq_bug is
    port (
        a  : in  STD_LOGIC_VECTOR(3 downto 0);
        b  : in  STD_LOGIC_VECTOR(3 downto 0);
        gt : out STD_LOGIC;
        eq : out STD_LOGIC;
        lt : out STD_LOGIC
    );
end entity cmp_eq_bug;

architecture behavioral of cmp_eq_bug is
begin
    gt <= '1' when unsigned(a) >  unsigned(b) else '0';
    -- BUG: eq always reports "not equal"
    eq <= '0';
    lt <= '1' when unsigned(a) <  unsigned(b) else '0';
end architecture behavioral;
