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
        elsif rising_edge(clk) then
            -- BUG: `load` is ignored — the register latches d on every
            -- rising edge, so q shadows d one cycle late whether or not
            -- load is asserted.
            q_int <= d;
        end if;
    end process;
    q <= q_int;
end architecture behavioral;
