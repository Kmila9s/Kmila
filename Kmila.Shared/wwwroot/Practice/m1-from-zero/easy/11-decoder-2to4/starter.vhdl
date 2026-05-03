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
    -- TODO: produce a one-hot output where the asserted bit's index = sel.
    y <= "0000";
end architecture behavioral;
