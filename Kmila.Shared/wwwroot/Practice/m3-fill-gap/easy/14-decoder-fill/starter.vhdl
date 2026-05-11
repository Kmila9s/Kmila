library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity decoder_y0_fill is
    port (
        sel : in  STD_LOGIC_VECTOR(1 downto 0);
        y   : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity decoder_y0_fill;

architecture behavioral of decoder_y0_fill is
    signal y0_int, y1_int, y2_int, y3_int : STD_LOGIC;
begin
    -- The other three lines are given:
    y1_int <= '1' when sel = "01" else '0';
    y2_int <= '1' when sel = "10" else '0';
    y3_int <= '1' when sel = "11" else '0';

    -- BEGIN_EDIT
    y0_int <= '0';  -- TODO: assert y0_int when sel = "00"
    -- END_EDIT

    y(0) <= y0_int;
    y(1) <= y1_int;
    y(2) <= y2_int;
    y(3) <= y3_int;
end architecture behavioral;
