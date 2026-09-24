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
    x_out <= std_logic_vector(signed(x_in) + signed(y_in)) when dir = '1'
        else std_logic_vector(signed(x_in) - signed(y_in));
end architecture behavioral;
