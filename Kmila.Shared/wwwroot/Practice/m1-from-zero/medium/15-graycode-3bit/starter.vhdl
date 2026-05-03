library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity gray3 is
    port (
        b : in  STD_LOGIC_VECTOR(2 downto 0);
        g : out STD_LOGIC_VECTOR(2 downto 0)
    );
end entity gray3;

architecture behavioral of gray3 is
begin
    -- TODO: convert binary b to Gray code g.
    g <= "000";
end architecture behavioral;
