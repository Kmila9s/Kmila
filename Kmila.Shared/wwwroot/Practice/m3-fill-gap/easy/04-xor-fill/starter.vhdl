library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity xor_fill is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity xor_fill;

architecture behavioral of xor_fill is
begin
    -- BEGIN_EDIT
    y <= '0';  -- TODO: implement y = a XOR b
    -- END_EDIT
end architecture behavioral;
