library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity edge_detector is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        d     : in  STD_LOGIC;
        pulse : out STD_LOGIC
    );
end entity edge_detector;

architecture behavioral of edge_detector is
    -- TODO: declare a signal d_prev that remembers d from the previous cycle.
begin
    -- TODO: pulse high for one clock when d went from 0 to 1.
    pulse <= '0';
end architecture behavioral;
