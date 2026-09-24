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
            if active = '0' then
                if start = '1' then
                    shreg  <= din;
                    phase  <= (others => '0');
                    active <= '1';
                end if;
            else
                if phase(0) = '1' then
                    shreg <= shreg(6 downto 0) & '0';
                end if;
                phase <= phase + 1;
            end if;
        end if;
    end process;

    sclk <= phase(0) when active = '1' else '0';
    mosi <= shreg(7) when active = '1' else '0';
end architecture behavioral;
