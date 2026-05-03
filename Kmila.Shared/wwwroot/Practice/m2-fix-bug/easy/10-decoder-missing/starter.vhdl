library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity dec_missing is
    port (
        sel : in  STD_LOGIC_VECTOR(1 downto 0);
        y   : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity dec_missing;

architecture behavioral of dec_missing is
begin
    y <= "0001" when sel = "00" else
         "0010" when sel = "01" else
         "0100" when sel = "10" else
         "0000";
end architecture behavioral;
