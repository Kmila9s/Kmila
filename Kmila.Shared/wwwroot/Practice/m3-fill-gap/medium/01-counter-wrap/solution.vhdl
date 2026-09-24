library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity counter_wrap_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        q     : out STD_LOGIC_VECTOR(2 downto 0)
    );
end entity counter_wrap_fill;

architecture behavioral of counter_wrap_fill is
    signal count : unsigned(2 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            count <= (others => '0');
        elsif rising_edge(clk) then
            if count = to_unsigned(7, 3) then
                count <= (others => '0');
            else
                count <= count + 1;
            end if;
        end if;
    end process;
    q <= std_logic_vector(count);
end architecture behavioral;
