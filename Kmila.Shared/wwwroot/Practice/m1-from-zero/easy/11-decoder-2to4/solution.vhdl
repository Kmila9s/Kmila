library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity decoder2to4 is
    port (
        sel : in  STD_LOGIC_VECTOR(1 downto 0);
        y   : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity decoder2to4;

architecture behavioral of decoder2to4 is
begin
    y <= "0001" when sel = "00" else
         "0010" when sel = "01" else
         "0100" when sel = "10" else
         "1000";
end architecture behavioral;
