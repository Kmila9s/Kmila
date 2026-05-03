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
    -- BUG: sensitivity list omits reset, so reset acts only on the next clock
    process(clk)
    begin
        if reset = '1' then
            count <= (others => '0');
        elsif rising_edge(clk) then
            count <= count + 1;
        end if;
    end process;
    q <= std_logic_vector(count);
end architecture behavioral;
