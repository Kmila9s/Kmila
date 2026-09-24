library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity udcounter_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        dir   : in  STD_LOGIC;
        q     : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity udcounter_fill;

architecture behavioral of udcounter_fill is
    signal count : unsigned(3 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            count <= (others => '0');
        elsif rising_edge(clk) then
            -- BEGIN_EDIT
            -- TODO: dir='1' increments count, dir='0' decrements count
            count <= count;
            -- END_EDIT
        end if;
    end process;
    q <= std_logic_vector(count);
end architecture behavioral;
