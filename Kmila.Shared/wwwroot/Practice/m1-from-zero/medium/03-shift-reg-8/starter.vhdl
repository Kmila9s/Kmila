library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity shift_reg8 is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        din   : in  STD_LOGIC;
        q     : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity shift_reg8;

architecture behavioral of shift_reg8 is
    -- TODO: declare an internal 8-bit register signal.
begin
    -- TODO: shift left and inject din at bit 0 on each rising edge.
    q <= (others => '0');
end architecture behavioral;
