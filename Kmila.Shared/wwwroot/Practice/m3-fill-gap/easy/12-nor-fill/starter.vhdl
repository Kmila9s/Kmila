library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity nor_fill is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity nor_fill;

architecture behavioral of nor_fill is
begin
    -- BEGIN_EDIT
    y <= '0';  -- TODO: implement y = a NOR b
    -- END_EDIT
end architecture behavioral;
