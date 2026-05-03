library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity and_fill is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity and_fill;

architecture behavioral of and_fill is
begin
    -- BEGIN_EDIT
    y <= '0';  -- TODO: implement y = a AND b
    -- END_EDIT
end architecture behavioral;
