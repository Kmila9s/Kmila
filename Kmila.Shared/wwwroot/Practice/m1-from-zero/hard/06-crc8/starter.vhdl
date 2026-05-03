library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity crc8_serial is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        din   : in  STD_LOGIC;
        crc   : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity crc8_serial;

architecture behavioral of crc8_serial is
    signal r : STD_LOGIC_VECTOR(7 downto 0) := (others => '0');
    constant POLY : STD_LOGIC_VECTOR(7 downto 0) := "00000111";
begin
    process(clk, reset)
        variable fb : STD_LOGIC;
    begin
        if reset = '1' then
            r <= (others => '0');
        elsif rising_edge(clk) then
            -- TODO: fb := r(7) xor din; shift left and xor POLY when fb='1'.
            null;
        end if;
    end process;

    crc <= r;
end architecture behavioral;
