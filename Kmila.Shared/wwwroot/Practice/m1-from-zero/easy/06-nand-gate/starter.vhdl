library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity nand_gate is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity nand_gate;

architecture behavioral of nand_gate is
begin
    -- TODO: implement a NAND gate.
    y <= '1';
end architecture behavioral;
