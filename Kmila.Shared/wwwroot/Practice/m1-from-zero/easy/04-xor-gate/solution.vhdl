library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity xor_gate is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity xor_gate;

architecture behavioral of xor_gate is
begin
    y <= a xor b;
end architecture behavioral;
