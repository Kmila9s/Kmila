library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity majority is
    port (
        a : in  STD_LOGIC;
        b : in  STD_LOGIC;
        c : in  STD_LOGIC;
        y : out STD_LOGIC
    );
end entity majority;

architecture behavioral of majority is
begin
    -- TODO: output the majority of a, b, c.
    y <= '0';
end architecture behavioral;
