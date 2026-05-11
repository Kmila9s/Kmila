library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity hamming_syndrome_fill is
    port (
        code : in  STD_LOGIC_VECTOR(6 downto 0);
        s    : out STD_LOGIC_VECTOR(2 downto 0)
    );
end entity hamming_syndrome_fill;

architecture behavioral of hamming_syndrome_fill is
begin
    s(0) <= code(6) xor code(4) xor code(2) xor code(0);
    s(1) <= code(5) xor code(4) xor code(1) xor code(0);
    s(2) <= code(3) xor code(2) xor code(1) xor code(0);
end architecture behavioral;
