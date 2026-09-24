library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity xnor_fill is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity xnor_fill;

architecture behavioral of xnor_fill is
begin
    -- BEGIN_EDIT
    y <= '0';  -- TODO: implement y = a XNOR b (true when a equals b)
    -- END_EDIT
end architecture behavioral;
