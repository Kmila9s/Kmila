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
    -- TODO: compute a - b on single bits.
    diff   <= '0';
    borrow <= '0';
end architecture behavioral;
