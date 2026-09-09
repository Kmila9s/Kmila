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
            -- BUG: empty is asserted whenever the stack is NON-empty
            -- (inverted logic). The consumer sees empty='0' at reset and
            -- empty='1' once the first push lands.
            if count = 0 then
                empty_reg <= '0';
            else
                empty_reg <= '1';
            end if;
        end if;
    end process;

    empty <= empty_reg;
end architecture behavioral;
