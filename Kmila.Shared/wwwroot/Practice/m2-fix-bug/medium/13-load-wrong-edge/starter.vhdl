library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity load_falling is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        load  : in  STD_LOGIC;
        d     : in  STD_LOGIC_VECTOR(3 downto 0);
        q     : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity load_falling;

architecture behavioral of load_falling is
    signal q_int : STD_LOGIC_VECTOR(3 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            q_int <= (others => '0');
        -- BUG: register is clocked on the falling edge instead of the rising edge
        elsif falling_edge(clk) then
            if load = '1' then
                q_int <= d;
            end if;
        end if;
    end process;
    q <= q_int;
end architecture behavioral;
