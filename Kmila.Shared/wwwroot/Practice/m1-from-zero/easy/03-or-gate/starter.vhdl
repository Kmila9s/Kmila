library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity or_gate is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity or_gate;

architecture behavioral of or_gate is
begin
    -- TODO: implement an OR gate.
    y <= '0';
end architecture behavioral;
