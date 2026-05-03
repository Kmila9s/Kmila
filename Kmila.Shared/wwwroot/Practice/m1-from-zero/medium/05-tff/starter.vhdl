library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity tff is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        t     : in  STD_LOGIC;
        q     : out STD_LOGIC
    );
end entity tff;

architecture behavioral of tff is
    -- TODO: declare an internal flip-flop signal qi.
begin
    -- TODO: toggle qi when t='1' on a rising edge; clear on async reset.
    q <= '0';
end architecture behavioral;
