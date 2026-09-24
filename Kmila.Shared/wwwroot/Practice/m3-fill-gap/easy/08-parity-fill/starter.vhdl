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
    -- BEGIN_EDIT
    p <= '0';  -- TODO: p = XOR of all four bits of d
    -- END_EDIT
end architecture behavioral;
