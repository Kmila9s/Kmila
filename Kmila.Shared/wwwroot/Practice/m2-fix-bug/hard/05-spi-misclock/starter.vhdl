library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity spi_clk is
    port (
        clk    : in  STD_LOGIC;
        reset  : in  STD_LOGIC;
        miso   : in  STD_LOGIC;
        sclk   : out STD_LOGIC;
        sample : out STD_LOGIC
    );
end entity spi_clk;

architecture behavioral of spi_clk is
    signal divider  : unsigned(2 downto 0) := (others => '0');
    signal sclk_int : STD_LOGIC := '0';
    signal sample_int : STD_LOGIC := '0';
begin
    process(clk, reset)
    begin
        if reset = '1' then
            divider    <= (others => '0');
            sclk_int   <= '0';
            sample_int <= '0';
        elsif rising_edge(clk) then
            divider <= divider + 1;
            sample_int <= miso;               -- BUG: samples on EVERY tick
                                              -- instead of only at the SCLK
                                              -- falling edge. `sample`
                                              -- shadows `miso` continuously.
            if divider = 3 then
                sclk_int   <= '1';            -- rising edge of SCLK
            elsif divider = 7 then
                sclk_int   <= '0';            -- falling edge of SCLK
                divider    <= (others => '0');
            end if;
        end if;
    end process;

    sclk   <= sclk_int;
    sample <= sample_int;
end architecture behavioral;
