library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity cordic_step is
    port (
        x_in  : in  STD_LOGIC_VECTOR(7 downto 0);
        y_in  : in  STD_LOGIC_VECTOR(7 downto 0);
        dir   : in  STD_LOGIC;
        x_out : out STD_LOGIC_VECTOR(7 downto 0);
        y_out : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity cordic_step;

architecture behavioral of cordic_step is
begin
    -- CCW when dir='1':  x' = x - y,  y' = y + x
    -- CW  when dir='0':  x' = x + y,  y' = y - x
    x_out <= std_logic_vector(signed(x_in) - signed(y_in)) when dir = '1'
        else std_logic_vector(signed(x_in) + signed(y_in));
    y_out <= std_logic_vector(signed(y_in) + signed(x_in)) when dir = '1'
        else std_logic_vector(signed(y_in) - signed(x_in));
end architecture behavioral;
