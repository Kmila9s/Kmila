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
    -- BUG: only the LSB of sel is used, so c and d are unreachable
    process(a, b, sel)
    begin
        case sel(0) is
            when '0'    => y <= a;
            when others => y <= b;
        end case;
    end process;
end architecture behavioral;
