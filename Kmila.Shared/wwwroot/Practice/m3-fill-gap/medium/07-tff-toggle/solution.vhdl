library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity tff_toggle_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        t     : in  STD_LOGIC;
        q     : out STD_LOGIC
    );
end entity tff_toggle_fill;

architecture behavioral of tff_toggle_fill is
    signal qi : STD_LOGIC := '0';
begin
    process(clk, reset)
    begin
        if reset = '1' then
            qi <= '0';
        elsif rising_edge(clk) then
            qi <= qi xor t;
        end if;
    end process;
    q <= qi;
end architecture behavioral;
