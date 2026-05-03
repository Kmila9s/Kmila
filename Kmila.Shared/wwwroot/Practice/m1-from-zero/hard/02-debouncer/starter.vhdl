library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity debouncer is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        raw   : in  STD_LOGIC;
        clean : out STD_LOGIC
    );
end entity debouncer;

architecture behavioral of debouncer is
    signal cnt    : unsigned(1 downto 0) := (others => '0');
    signal stable : STD_LOGIC := '0';
    signal last   : STD_LOGIC := '0';
begin
    process(clk, reset)
    begin
        if reset = '1' then
            cnt    <= (others => '0');
            stable <= '0';
            last   <= '0';
        elsif rising_edge(clk) then
            -- TODO: count consecutive cycles where raw = last; flip stable when cnt saturates.
            null;
        end if;
    end process;
    clean <= stable;
end architecture behavioral;
