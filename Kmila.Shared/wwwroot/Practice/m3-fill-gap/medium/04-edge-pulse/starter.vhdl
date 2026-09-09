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

    -- BEGIN_EDIT
    -- TODO: emit a one-cycle pulse on the rising edge of d.
    -- (Common first-attempt stub — just passes d through — doesn't
    -- produce a pulse; students must combine d_curr and d_prev.)
    pulse <= d;
    -- END_EDIT
end architecture behavioral;
