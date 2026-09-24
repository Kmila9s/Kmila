library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity ha_swap is
    port (
        a    : in  STD_LOGIC;
        b    : in  STD_LOGIC;
        sum  : out STD_LOGIC;
        cout : out STD_LOGIC
    );
end entity ha_swap;

architecture behavioral of ha_swap is
begin
    sum  <= a and b;
    cout <= a xor b;
end architecture behavioral;
