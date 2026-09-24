library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity mux4_narrow is
    port (
        a   : in  STD_LOGIC;
        b   : in  STD_LOGIC;
        c   : in  STD_LOGIC;
        d   : in  STD_LOGIC;
        sel : in  STD_LOGIC_VECTOR(1 downto 0);
        y   : out STD_LOGIC
    );
end entity mux4_narrow;

architecture behavioral of mux4_narrow is
begin
    process(a, b, c, d, sel)
    begin
        case sel is
            when "00"   => y <= a;
            when "01"   => y <= b;
            when "10"   => y <= c;
            when others => y <= d;
        end case;
    end process;
end architecture behavioral;
