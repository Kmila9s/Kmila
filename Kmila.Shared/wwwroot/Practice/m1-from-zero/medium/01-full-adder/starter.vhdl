library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity full_adder is
    port (
        a    : in  STD_LOGIC;
        b    : in  STD_LOGIC;
        cin  : in  STD_LOGIC;
        sum  : out STD_LOGIC;
        cout : out STD_LOGIC
    );
end entity full_adder;

architecture behavioral of full_adder is
begin
    -- TODO: implement sum and cout for a 1-bit full adder.
    sum  <= '0';
    cout <= '0';
end architecture behavioral;
