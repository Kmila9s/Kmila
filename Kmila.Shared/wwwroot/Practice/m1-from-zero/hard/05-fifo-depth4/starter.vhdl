library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity fifo_flags is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        push  : in  STD_LOGIC;
        pop   : in  STD_LOGIC;
        full  : out STD_LOGIC;
        empty : out STD_LOGIC
    );
end entity fifo_flags;

architecture behavioral of fifo_flags is
    signal cnt : unsigned(2 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            cnt <= (others => '0');
        elsif rising_edge(clk) then
            -- TODO: increment on push (clamped at 4), decrement on pop (clamped at 0).
            null;
        end if;
    end process;

    -- TODO: drive full / empty from cnt.
    full  <= '0';
    empty <= '0';
end architecture behavioral;
