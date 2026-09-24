library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity counter_noreset is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        q     : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity counter_noreset;

architecture behavioral of counter_noreset is
    signal count : unsigned(3 downto 0) := (others => '0');
begin
    -- BUG: the reset branch clears to all-ones instead of zero, so the
    -- counter starts at 15 and wraps to 0 on the next tick instead of the
    -- expected 0 → 1 → 2 sequence.
    process(clk, reset)
    begin
        if reset = '1' then
            count <= (others => '1');
        elsif rising_edge(clk) then
            count <= count + 1;
        end if;
    end process;
    q <= std_logic_vector(count);
end architecture behavioral;
