library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity tff is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        t     : in  STD_LOGIC;
        q     : out STD_LOGIC
    );
end entity tff;

architecture behavioral of tff is
    signal qi : std_logic := '0';
begin
    process(clk, reset)
    begin
        if reset = '1' then
            qi <= '0';
        elsif rising_edge(clk) then
            if t = '1' then
                qi <= not qi;
            end if;
        end if;
    end process;
    q <= qi;
end architecture behavioral;
