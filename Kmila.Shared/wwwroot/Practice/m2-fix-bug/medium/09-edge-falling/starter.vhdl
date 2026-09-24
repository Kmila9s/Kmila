library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity edge_falling is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        d     : in  STD_LOGIC;
        pulse : out STD_LOGIC
    );
end entity edge_falling;

architecture behavioral of edge_falling is
    signal d_prev : STD_LOGIC := '0';
begin
    process(clk, reset)
    begin
        if reset = '1' then
            d_prev <= '0';
        elsif rising_edge(clk) then
            d_prev <= d;
        end if;
    end process;

    -- BUG: this detects the FALLING edge ('1' -> '0'), not the rising edge
    pulse <= '1' when (d = '0' and d_prev = '1') else '0';
end architecture behavioral;
