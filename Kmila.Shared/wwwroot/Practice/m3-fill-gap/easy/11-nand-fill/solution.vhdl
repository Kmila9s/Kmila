library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity nand_fill is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity nand_fill;

architecture behavioral of nand_fill is
begin
    y <= a nand b;
end architecture behavioral;
