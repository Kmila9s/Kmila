library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity hamming_swap is
    port (
        d    : in  STD_LOGIC_VECTOR(3 downto 0);
        code : out STD_LOGIC_VECTOR(6 downto 0)
    );
end entity hamming_swap;

architecture behavioral of hamming_swap is
    signal p1, p2, p3 : STD_LOGIC;
begin
    p1 <= d(0) xor d(1) xor d(3);
    p2 <= d(0) xor d(2) xor d(3);
    p3 <= d(1) xor d(2) xor d(3);
    -- code(6 downto 0) = p1 & p2 & d(0) & p3 & d(1) & d(2) & d(3)
    code <= p1 & p2 & d(0) & p3 & d(1) & d(2) & d(3);
end architecture behavioral;
