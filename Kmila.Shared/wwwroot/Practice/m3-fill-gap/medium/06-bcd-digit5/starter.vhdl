library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity bcd5_fill is
    port (
        digit : in  STD_LOGIC_VECTOR(3 downto 0);
        seg   : out STD_LOGIC_VECTOR(6 downto 0)
    );
end entity bcd5_fill;

architecture behavioral of bcd5_fill is
begin
    process(digit)
    begin
        case digit is
            when "0000" => seg <= "1111110";
            when "0001" => seg <= "0110000";
            when "0010" => seg <= "1101101";
            when "0011" => seg <= "1111001";
            when "0100" => seg <= "0110011";
            -- BEGIN_EDIT
            -- TODO: produce the 7-segment pattern for digit 5 (a-g, active high)
            when "0101" => seg <= "0000000";
            -- END_EDIT
            when "0110" => seg <= "1011111";
            when "0111" => seg <= "1110000";
            when "1000" => seg <= "1111111";
            when "1001" => seg <= "1111011";
            when others => seg <= "0000000";
        end case;
    end process;
end architecture behavioral;
