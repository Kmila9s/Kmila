library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity half_adder is
    port (
        a   : in  STD_LOGIC;
        b   : in  STD_LOGIC;
        sum : out STD_LOGIC;
        cout: out STD_LOGIC
    );
end entity half_adder;

architecture behavioral of half_adder is
begin
    -- TODO: assign sum and cout from a and b.
    sum  <= '0';
    cout <= '0';
end architecture behavioral;
