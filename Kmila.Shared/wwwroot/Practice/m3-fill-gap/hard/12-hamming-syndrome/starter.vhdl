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
    -- BEGIN_EDIT
    s <= "000";  -- TODO: compute the 3-bit Hamming(7,4) syndrome
    -- END_EDIT
end architecture behavioral;
