library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity priority_encoder42 is
    port (
        d : in  STD_LOGIC_VECTOR(3 downto 0);
        y : out STD_LOGIC_VECTOR(1 downto 0)
    );
end entity priority_encoder42;

architecture behavioral of priority_encoder42 is
begin
    y <= "11" when d(3) = '1' else
         "10" when d(2) = '1' else
         "01" when d(1) = '1' else
         "00";
end architecture behavioral;
