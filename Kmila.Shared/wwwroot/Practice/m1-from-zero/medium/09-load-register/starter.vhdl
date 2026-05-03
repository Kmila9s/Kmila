library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity load_register is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        load  : in  STD_LOGIC;
        d     : in  STD_LOGIC_VECTOR(3 downto 0);
        q     : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity load_register;

architecture behavioral of load_register is
    -- TODO: declare a 4-bit internal register signal r.
begin
    -- TODO: load d into r when load='1' on rising edge; clear on async reset.
    q <= (others => '0');
end architecture behavioral;
