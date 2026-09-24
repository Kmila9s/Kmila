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
    signal d_prev : std_logic := '0';
begin
    process(clk, reset)
    begin
        if reset = '1' then
            d_prev <= '0';
        elsif rising_edge(clk) then
            d_prev <= d;
        end if;
    end process;
    pulse <= '1' when (d = '1' and d_prev = '0') else '0';
end architecture behavioral;
