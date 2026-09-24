library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity add2_buggy is
    port (
        a    : in  STD_LOGIC_VECTOR(1 downto 0);
        b    : in  STD_LOGIC_VECTOR(1 downto 0);
        cin  : in  STD_LOGIC;
        sum  : out STD_LOGIC_VECTOR(1 downto 0);
        cout : out STD_LOGIC
    );
end entity add2_buggy;

architecture behavioral of add2_buggy is
begin
    -- Each bit treated independently — intra-bit carry is ignored.
    sum(0) <= a(0) xor b(0) xor cin;
    sum(1) <= a(1) xor b(1);
    cout   <= a(1) and b(1);
end architecture behavioral;
