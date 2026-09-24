library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity or_fill is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity or_fill;

architecture behavioral of or_fill is
begin
    -- BEGIN_EDIT
    y <= '0';  -- TODO: implement y = a OR b
    -- END_EDIT
end architecture behavioral;
