library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity mult4 is
    port (
        a       : in  STD_LOGIC_VECTOR(3 downto 0);
        b       : in  STD_LOGIC_VECTOR(3 downto 0);
        product : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity mult4;

architecture behavioral of mult4 is
begin
    product <= std_logic_vector(unsigned(a) * unsigned(b));
end architecture behavioral;
