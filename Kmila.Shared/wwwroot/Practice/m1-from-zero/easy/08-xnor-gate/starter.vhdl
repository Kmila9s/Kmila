library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity xnor_gate is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity xnor_gate;

architecture behavioral of xnor_gate is
begin
    -- TODO: implement an XNOR gate.
    y <= '1';
end architecture behavioral;
