library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity hamming74 is
    port (
        d    : in  STD_LOGIC_VECTOR(3 downto 0);
        code : out STD_LOGIC_VECTOR(6 downto 0)
    );
end entity hamming74;

architecture behavioral of hamming74 is
    signal p1, p2, p3 : STD_LOGIC;
begin
    -- TODO: parity bits
    -- p1 = d0 xor d1 xor d3
    -- p2 = d0 xor d2 xor d3
    -- p3 = d1 xor d2 xor d3
    p1 <= '0';
    p2 <= '0';
    p3 <= '0';

    -- TODO: pack as p1 & p2 & d0 & p3 & d1 & d2 & d3
    code <= (others => '0');
end architecture behavioral;
