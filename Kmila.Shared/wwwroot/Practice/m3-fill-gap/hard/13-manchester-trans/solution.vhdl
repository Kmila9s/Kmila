library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity manchester_trans_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        din   : in  STD_LOGIC;
        phase : in  STD_LOGIC;
        m     : out STD_LOGIC
    );
end entity manchester_trans_fill;

architecture behavioral of manchester_trans_fill is
begin
    m <= din xor phase;
end architecture behavioral;
