library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity alu_shift_fill is
    port (
        a  : in  STD_LOGIC_VECTOR(7 downto 0);
        sh : in  STD_LOGIC_VECTOR(2 downto 0);
        y  : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity alu_shift_fill;

architecture behavioral of alu_shift_fill is
begin
    process(a, sh)
    begin
        -- BEGIN_EDIT
        -- TODO: assign y to a shifted left by sh positions.
        -- Hint: shift_left, unsigned, to_integer, std_logic_vector.
        y <= a;
        -- END_EDIT
    end process;
end architecture behavioral;
