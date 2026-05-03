library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity parity_fill is
    port (
        d : in  STD_LOGIC_VECTOR(3 downto 0);
        p : out STD_LOGIC
    );
end entity parity_fill;

architecture behavioral of parity_fill is
begin
    p <= d(0) xor d(1) xor d(2) xor d(3);
end architecture behavioral;
