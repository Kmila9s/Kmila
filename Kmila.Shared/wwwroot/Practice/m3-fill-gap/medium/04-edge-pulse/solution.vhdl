library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity edge_pulse_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        d     : in  STD_LOGIC;
        pulse : out STD_LOGIC
    );
end entity edge_pulse_fill;

architecture behavioral of edge_pulse_fill is
    signal d_curr : STD_LOGIC := '0';
    signal d_prev : STD_LOGIC := '0';
begin
    process(clk, reset)
    begin
        if reset = '1' then
            d_curr <= '0';
            d_prev <= '0';
        elsif rising_edge(clk) then
            d_prev <= d_curr;
            d_curr <= d;
        end if;
    end process;

    pulse <= d_curr and (not d_prev);
end architecture behavioral;
