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
        -- TODO: select add/sub/and/or with op.
        r <= (others => '0');
    end process;
    y <= std_logic_vector(r);
end architecture behavioral;
