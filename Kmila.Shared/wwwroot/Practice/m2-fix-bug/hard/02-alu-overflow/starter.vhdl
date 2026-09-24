library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity alu_ovf is
    port (
        a   : in  STD_LOGIC_VECTOR(7 downto 0);
        b   : in  STD_LOGIC_VECTOR(7 downto 0);
        ovf : out STD_LOGIC
    );
end entity alu_ovf;

architecture behavioral of alu_ovf is
    signal sum : STD_LOGIC_VECTOR(7 downto 0);
begin
    sum <= std_logic_vector(signed(a) + signed(b));
    -- BUG: indexes the second-most-significant bit (6) instead of the
    -- sign bit (7). Overflow detection becomes nonsense.
    ovf <= '1' when (a(6) = b(6)) and (sum(6) /= a(6)) else '0';
end architecture behavioral;
