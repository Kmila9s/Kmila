library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity enc_buggy is
    port (
        d : in  STD_LOGIC_VECTOR(3 downto 0);
        y : out STD_LOGIC_VECTOR(1 downto 0)
    );
end entity enc_buggy;

architecture behavioral of enc_buggy is
begin
    y <= "00" when d(0) = '1' else
         "01" when d(1) = '1' else
         "10" when d(2) = '1' else
         "11" when d(3) = '1' else
         "00";
end architecture behavioral;
