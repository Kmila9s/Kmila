library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity not_fill is
    port (
        a : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity not_fill;

architecture behavioral of not_fill is
begin
    -- BEGIN_EDIT
    y <= '0';  -- TODO: implement y = NOT a
    -- END_EDIT
end architecture behavioral;
