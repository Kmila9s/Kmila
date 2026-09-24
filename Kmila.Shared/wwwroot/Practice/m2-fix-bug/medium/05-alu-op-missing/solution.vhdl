library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity alu_op_missing is
    port (
        a  : in  STD_LOGIC_VECTOR(3 downto 0);
        b  : in  STD_LOGIC_VECTOR(3 downto 0);
        op : in  STD_LOGIC_VECTOR(1 downto 0);
        y  : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity alu_op_missing;

architecture behavioral of alu_op_missing is
begin
    process(a, b, op)
    begin
        case op is
            when "00"   => y <= std_logic_vector(unsigned(a) + unsigned(b));
            when "01"   => y <= std_logic_vector(unsigned(a) - unsigned(b));
            when "10"   => y <= a and b;
            when others => y <= a xor b;
        end case;
    end process;
end architecture behavioral;
