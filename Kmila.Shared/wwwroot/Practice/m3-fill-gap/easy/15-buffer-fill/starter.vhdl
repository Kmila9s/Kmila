library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity tristate_fill is
    port (
        a  : in  STD_LOGIC;
        en : in  STD_LOGIC;
        y  : out STD_LOGIC
    );
end entity tristate_fill;

architecture behavioral of tristate_fill is
begin
    -- BEGIN_EDIT
    y <= '0';  -- TODO: y = a when en='1', else 'Z'
    -- END_EDIT
end architecture behavioral;
