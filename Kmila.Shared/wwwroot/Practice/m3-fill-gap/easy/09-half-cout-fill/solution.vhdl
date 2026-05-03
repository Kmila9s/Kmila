library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity ha_cout_fill is
    port (
        a    : in  STD_LOGIC;
        b    : in  STD_LOGIC;
        sum  : out STD_LOGIC;
        cout : out STD_LOGIC
    );
end entity ha_cout_fill;

architecture behavioral of ha_cout_fill is
begin
    sum  <= a xor b;
    cout <= a and b;
end architecture behavioral;
