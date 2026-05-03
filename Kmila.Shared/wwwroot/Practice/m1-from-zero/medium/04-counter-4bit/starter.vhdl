library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity counter4 is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        q     : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity counter4;

architecture behavioral of counter4 is
    -- TODO: declare a 4-bit unsigned count signal initialised to zeros.
begin
    -- TODO: increment count on rising edge; clear on async reset.
    q <= (others => '0');
end architecture behavioral;
