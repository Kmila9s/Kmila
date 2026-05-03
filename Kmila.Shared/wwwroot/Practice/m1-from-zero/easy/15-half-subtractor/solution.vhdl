library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity half_subtractor is
    port (
        a      : in  STD_LOGIC;
        b      : in  STD_LOGIC;
        diff   : out STD_LOGIC;
        borrow : out STD_LOGIC
    );
end entity half_subtractor;

architecture behavioral of half_subtractor is
begin
    diff   <= a xor b;
    borrow <= (not a) and b;
end architecture behavioral;
