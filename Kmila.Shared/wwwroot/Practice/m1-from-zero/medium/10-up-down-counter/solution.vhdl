library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity up_down_counter4 is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        dir   : in  STD_LOGIC;
        q     : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity up_down_counter4;

architecture behavioral of up_down_counter4 is
    signal count : unsigned(3 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            count <= (others => '0');
        elsif rising_edge(clk) then
            if dir = '1' then
                count <= count + 1;
            else
                count <= count - 1;
            end if;
        end if;
    end process;
    q <= std_logic_vector(count);
end architecture behavioral;
