library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity cmp_eq_fill is
    port (
        a  : in  STD_LOGIC_VECTOR(3 downto 0);
        b  : in  STD_LOGIC_VECTOR(3 downto 0);
        eq : out STD_LOGIC
    );
end entity cmp_eq_fill;

architecture behavioral of cmp_eq_fill is
begin
    -- BEGIN_EDIT
    eq <= '0';  -- TODO: eq = '1' when a equals b, else '0'
    -- END_EDIT
end architecture behavioral;
