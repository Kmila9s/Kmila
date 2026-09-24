library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity cmp_eq_buggy is
    port (
        a  : in  STD_LOGIC_VECTOR(3 downto 0);
        b  : in  STD_LOGIC_VECTOR(3 downto 0);
        eq : out STD_LOGIC
    );
end entity cmp_eq_buggy;

architecture behavioral of cmp_eq_buggy is
begin
    eq <= '1' when a(2 downto 0) = b(2 downto 0) else '0';
end architecture behavioral;
