library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity clk_divider4 is
    port (
        clk     : in  STD_LOGIC;
        reset   : in  STD_LOGIC;
        clk_out : out STD_LOGIC
    );
end entity clk_divider4;

architecture behavioral of clk_divider4 is
    -- TODO: declare a 2-bit counter signal cnt.
begin
    -- TODO: increment cnt every rising edge; output the high bit as clk_out.
    clk_out <= '0';
end architecture behavioral;
