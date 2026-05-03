library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity toggle_reg is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        en    : in  STD_LOGIC;
        q     : out STD_LOGIC
    );
end entity toggle_reg;

architecture behavioral of toggle_reg is
    -- TODO: declare an internal qi signal initialised to '0'.
begin
    -- TODO: when en='1', flip qi each rising edge; clear on async reset.
    q <= '0';
end architecture behavioral;
