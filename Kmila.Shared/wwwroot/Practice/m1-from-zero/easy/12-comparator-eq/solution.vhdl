library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity comparator_eq is
    port (
        a  : in  STD_LOGIC_VECTOR(3 downto 0);
        b  : in  STD_LOGIC_VECTOR(3 downto 0);
        eq : out STD_LOGIC
    );
end entity comparator_eq;

architecture behavioral of comparator_eq is
begin
    eq <= '1' when a = b else '0';
end architecture behavioral;
