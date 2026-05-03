library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity stack_flags is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        push  : in  STD_LOGIC;
        pop   : in  STD_LOGIC;
        full  : out STD_LOGIC;
        empty : out STD_LOGIC
    );
end entity stack_flags;

architecture behavioral of stack_flags is
    signal cnt : unsigned(2 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            cnt <= (others => '0');
        elsif rising_edge(clk) then
            -- TODO: depth-4 stack: increment on push (clamp at 4), decrement on pop (clamp at 0).
            null;
        end if;
    end process;

    -- TODO: drive flags from cnt.
    full  <= '0';
    empty <= '0';
end architecture behavioral;
