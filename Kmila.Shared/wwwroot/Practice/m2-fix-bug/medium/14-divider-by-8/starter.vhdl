library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity divider_buggy is
    port (
        clk     : in  STD_LOGIC;
        reset   : in  STD_LOGIC;
        clk_out : out STD_LOGIC
    );
end entity divider_buggy;

architecture behavioral of divider_buggy is
    signal cnt : unsigned(2 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            cnt <= (others => '0');
        elsif rising_edge(clk) then
            cnt <= cnt + 1;
        end if;
    end process;
    -- BUG: cnt(2) divides by 8, not by 4
    clk_out <= cnt(2);
end architecture behavioral;
