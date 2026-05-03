library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity parity_even is
    port (
        d : in  STD_LOGIC_VECTOR(3 downto 0);
        p : out STD_LOGIC
    );
end entity parity_even;

architecture behavioral of parity_even is
begin
    -- TODO: produce the parity bit so total ones in d & p is even.
    p <= '0';
end architecture behavioral;
