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
    -- TODO: implement one CORDIC iteration on signed 8-bit values.
    x_out <= (others => '0');
    y_out <= (others => '0');
end architecture behavioral;
