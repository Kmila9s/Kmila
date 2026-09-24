library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity onehot_to_bin is
    port (
        oh : in  STD_LOGIC_VECTOR(3 downto 0);
        b  : out STD_LOGIC_VECTOR(1 downto 0)
    );
end entity onehot_to_bin;

architecture behavioral of onehot_to_bin is
begin
    b <= "00" when oh = "0001" else
         "01" when oh = "0010" else
         "10" when oh = "0100" else
         "11";
end architecture behavioral;
