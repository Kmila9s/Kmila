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
    -- TODO: declare the count signal as unsigned(3 downto 0).
begin
    -- TODO: count up when dir='1', down when dir='0'; async reset clears.
    q <= (others => '0');
end architecture behavioral;
