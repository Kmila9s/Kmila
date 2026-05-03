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
            if push = '1' and pop = '0' and cnt < to_unsigned(4, 3) then
                cnt <= cnt + 1;
            elsif pop = '1' and push = '0' and cnt > 0 then
                cnt <= cnt - 1;
            end if;
        end if;
    end process;

    full  <= '1' when cnt = to_unsigned(4, 3) else '0';
    empty <= '1' when cnt = 0 else '0';
end architecture behavioral;
