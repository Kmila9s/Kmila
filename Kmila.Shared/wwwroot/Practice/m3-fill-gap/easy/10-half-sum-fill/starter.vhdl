library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity ha_sum_fill is
    port (
        a    : in  STD_LOGIC;
        b    : in  STD_LOGIC;
        sum  : out STD_LOGIC;
        cout : out STD_LOGIC
    );
end entity ha_sum_fill;

architecture behavioral of ha_sum_fill is
begin
    cout <= a and b;
    -- BEGIN_EDIT
    sum  <= '0';  -- TODO: sum = a XOR b (sum bit of a half-adder)
    -- END_EDIT
end architecture behavioral;
