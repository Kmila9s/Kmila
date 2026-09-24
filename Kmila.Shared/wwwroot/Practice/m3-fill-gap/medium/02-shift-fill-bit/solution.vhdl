library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity shift_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        din   : in  STD_LOGIC;
        mode  : in  STD_LOGIC;
        q     : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity shift_fill;

architecture behavioral of shift_fill is
    signal sreg : STD_LOGIC_VECTOR(7 downto 0) := (others => '0');
    signal feed : STD_LOGIC;
begin
    feed <= din when mode = '1' else '0';

    process(clk, reset)
    begin
        if reset = '1' then
            sreg <= (others => '0');
        elsif rising_edge(clk) then
            sreg <= sreg(6 downto 0) & feed;
        end if;
    end process;
    q <= sreg;
end architecture behavioral;
