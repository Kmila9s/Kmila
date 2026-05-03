library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity reg_async_as_sync is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        d     : in  STD_LOGIC;
        q     : out STD_LOGIC
    );
end entity reg_async_as_sync;

architecture behavioral of reg_async_as_sync is
begin
    process(clk, reset)
    begin
        if reset = '1' then
            q <= '0';
        elsif rising_edge(clk) then
            q <= d;
        end if;
    end process;
end architecture behavioral;
