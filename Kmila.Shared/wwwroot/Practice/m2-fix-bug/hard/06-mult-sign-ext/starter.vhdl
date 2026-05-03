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
    -- BUG: `a` is sign-extended (signed) while `b` is treated as
    -- unsigned. When a's MSB is 1 the operand becomes a negative
    -- number and the product is wrong.
    product <= std_logic_vector(resize(signed(a) * signed('0' & b), 8));
end architecture behavioral;
