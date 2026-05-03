library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity mult_signext is
    port (
        a       : in  STD_LOGIC_VECTOR(3 downto 0);
        b       : in  STD_LOGIC_VECTOR(3 downto 0);
        product : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity mult_signext;

architecture behavioral of mult_signext is
begin
    -- Both operands are interpreted as UNSIGNED 4-bit numbers, so
    -- the 8-bit product is the textbook unsigned multiplication.
    product <= std_logic_vector(unsigned(a) * unsigned(b));
end architecture behavioral;
