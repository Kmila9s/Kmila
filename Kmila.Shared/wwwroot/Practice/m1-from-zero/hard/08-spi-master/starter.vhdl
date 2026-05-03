library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity spi_master is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        start : in  STD_LOGIC;
        din   : in  STD_LOGIC_VECTOR(7 downto 0);
        sclk  : out STD_LOGIC;
        mosi  : out STD_LOGIC
    );
end entity spi_master;

architecture behavioral of spi_master is
    signal phase  : unsigned(3 downto 0) := (others => '0');
    signal shreg  : STD_LOGIC_VECTOR(7 downto 0) := (others => '0');
    signal active : STD_LOGIC := '0';
begin
    process(clk, reset)
    begin
        if reset = '1' then
            phase  <= (others => '0');
            shreg  <= (others => '0');
            active <= '0';
        elsif rising_edge(clk) then
            -- TODO: latch din on start, then increment phase and shift shreg every odd phase.
            null;
        end if;
    end process;

    -- TODO: drive sclk = phase(0) and mosi = shreg(7) while active.
    sclk <= '0';
    mosi <= '0';
end architecture behavioral;
