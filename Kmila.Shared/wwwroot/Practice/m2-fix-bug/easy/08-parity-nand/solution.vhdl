library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity parity_buggy is
    port (
        d : in  STD_LOGIC_VECTOR(3 downto 0);
        p : out STD_LOGIC
    );
end entity parity_buggy;

architecture behavioral of parity_buggy is
begin
    p <= d(0) xor d(1) xor d(2) xor d(3);
end architecture behavioral;
