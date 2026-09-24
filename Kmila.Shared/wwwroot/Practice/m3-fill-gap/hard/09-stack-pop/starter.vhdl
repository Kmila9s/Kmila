library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity stack_pop_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        push  : in  STD_LOGIC;
        pop   : in  STD_LOGIC;
        empty : out STD_LOGIC
    );
end entity stack_pop_fill;

architecture behavioral of stack_pop_fill is
    signal sp : unsigned(3 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            sp <= (others => '0');
        elsif rising_edge(clk) then
            if push = '1' then
                sp <= sp + 1;
            end if;
            -- BEGIN_EDIT
            -- TODO: decrement sp on pop, but only when the stack
            -- is not already empty (sp /= 0).
            if pop = '1' then
                sp <= sp;
            end if;
            -- END_EDIT
        end if;
    end process;

    empty <= '1' when sp = 0 else '0';
end architecture behavioral;
