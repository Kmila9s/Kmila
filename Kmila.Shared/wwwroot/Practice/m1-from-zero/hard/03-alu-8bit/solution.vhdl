library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity alu8 is
    port (
        a  : in  STD_LOGIC_VECTOR(7 downto 0);
        b  : in  STD_LOGIC_VECTOR(7 downto 0);
        op : in  STD_LOGIC_VECTOR(1 downto 0);
        y  : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity alu8;

architecture behavioral of alu8 is
    signal r : unsigned(7 downto 0);
begin
    process(a, b, op)
    begin
        case op is
            when "00"   => r <= unsigned(a) + unsigned(b);
            when "01"   => r <= unsigned(a) - unsigned(b);
            when "10"   => r <= unsigned(a) and unsigned(b);
            when others => r <= unsigned(a) or  unsigned(b);
        end case;
    end process;
    y <= std_logic_vector(r);
end architecture behavioral;
