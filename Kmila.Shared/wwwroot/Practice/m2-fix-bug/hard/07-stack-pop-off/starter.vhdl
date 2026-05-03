library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity stack_off is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        push  : in  STD_LOGIC;
        pop   : in  STD_LOGIC;
        empty : out STD_LOGIC
    );
end entity stack_off;

architecture behavioral of stack_off is
    signal count       : unsigned(3 downto 0) := (others => '0');
    signal empty_reg   : STD_LOGIC := '1';
begin
    process(clk, reset)
    begin
        if reset = '1' then
            count     <= (others => '0');
            empty_reg <= '1';
        elsif rising_edge(clk) then
            if push = '1' and pop = '0' then
                count <= count + 1;
            elsif pop = '1' and push = '0' and count > 0 then
                count <= count - 1;
            end if;
            -- BUG: empty is registered using the OLD count value,
            -- so it lags one cycle behind the actual stack level.
            if count = 0 then
                empty_reg <= '1';
            else
                empty_reg <= '0';
            end if;
        end if;
    end process;

    empty <= empty_reg;
end architecture behavioral;
