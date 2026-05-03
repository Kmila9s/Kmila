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
    -- TODO: encode the highest active bit of d into the 2-bit y.
    y <= "00";
end architecture behavioral;
