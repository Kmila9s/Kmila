library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity cordic_x_fill is
    port (
        x_in  : in  STD_LOGIC_VECTOR(7 downto 0);
        y_in  : in  STD_LOGIC_VECTOR(7 downto 0);
        dir   : in  STD_LOGIC;
        x_out : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity cordic_x_fill;

architecture behavioral of cordic_x_fill is
begin
    -- BEGIN_EDIT
    x_out <= (others => '0');  -- TODO: x' = x + y when dir='1', else x - y
    -- END_EDIT
end architecture behavioral;
