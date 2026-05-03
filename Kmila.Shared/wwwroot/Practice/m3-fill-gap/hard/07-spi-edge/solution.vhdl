library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity spi_edge_fill is
    port (
        clk    : in  STD_LOGIC;
        reset  : in  STD_LOGIC;
        sclk   : in  STD_LOGIC;
        sample : out STD_LOGIC
    );
end entity spi_edge_fill;

architecture behavioral of spi_edge_fill is
    signal sclk_prev : STD_LOGIC := '0';
begin
    process(clk, reset)
    begin
        if reset = '1' then
            sclk_prev <= '0';
        elsif rising_edge(clk) then
            sclk_prev <= sclk;
        end if;
    end process;

    -- BEGIN_EDIT
    sample <= sclk_prev and (not sclk);
    -- END_EDIT
end architecture behavioral;
