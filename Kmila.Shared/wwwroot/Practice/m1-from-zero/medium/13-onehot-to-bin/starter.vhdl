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
    -- TODO: convert the one-hot value oh into the 2-bit index b.
    b <= "00";
end architecture behavioral;
