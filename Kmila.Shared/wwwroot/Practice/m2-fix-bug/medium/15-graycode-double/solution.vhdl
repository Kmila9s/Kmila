library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity gray_buggy is
    port (
        b : in  STD_LOGIC_VECTOR(2 downto 0);
        g : out STD_LOGIC_VECTOR(2 downto 0)
    );
end entity gray_buggy;

architecture behavioral of gray_buggy is
begin
    g(2) <= b(2);
    g(1) <= b(2) xor b(1);
    g(0) <= b(1) xor b(0);
end architecture behavioral;
