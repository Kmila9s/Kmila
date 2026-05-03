library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity adder_signext is
    port (
        a   : in  STD_LOGIC_VECTOR(3 downto 0);
        b   : in  STD_LOGIC_VECTOR(7 downto 0);
        sum : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity adder_signext;

architecture behavioral of adder_signext is
    signal a_ext : signed(7 downto 0);
begin
    a_ext <= resize(signed(a), 8);
    sum   <= std_logic_vector(a_ext + signed(b));
end architecture behavioral;
