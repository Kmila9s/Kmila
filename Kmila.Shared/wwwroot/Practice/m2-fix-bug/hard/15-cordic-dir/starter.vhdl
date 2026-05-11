library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity cordic_dir_buggy is
    port (
        x_in  : in  STD_LOGIC_VECTOR(7 downto 0);
        y_in  : in  STD_LOGIC_VECTOR(7 downto 0);
        dir   : in  STD_LOGIC;
        x_out : out STD_LOGIC_VECTOR(7 downto 0);
        y_out : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity cordic_dir_buggy;

architecture behavioral of cordic_dir_buggy is
begin
    -- BUG: dir senses inverted — should rotate CCW when dir='1', this rotates CW.
    x_out <= std_logic_vector(signed(x_in) - signed(y_in)) when dir = '0'
        else std_logic_vector(signed(x_in) + signed(y_in));
    y_out <= std_logic_vector(signed(y_in) + signed(x_in)) when dir = '0'
        else std_logic_vector(signed(y_in) - signed(x_in));
end architecture behavioral;
