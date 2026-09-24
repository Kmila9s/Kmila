library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity shift_wrong is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        din   : in  STD_LOGIC;
        q     : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity shift_wrong;

architecture behavioral of shift_wrong is
    signal r : STD_LOGIC_VECTOR(7 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            r <= (others => '0');
        elsif rising_edge(clk) then
            -- Shift LEFT: din enters at LSB, MSB falls off
            r <= r(6 downto 0) & din;
        end if;
    end process;
    q <= r;
end architecture behavioral;
