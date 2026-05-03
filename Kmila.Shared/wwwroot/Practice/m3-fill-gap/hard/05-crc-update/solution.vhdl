library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity crc_update_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        din   : in  STD_LOGIC;
        crc   : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity crc_update_fill;

architecture behavioral of crc_update_fill is
    signal r : STD_LOGIC_VECTOR(7 downto 0) := (others => '0');
    constant POLY : STD_LOGIC_VECTOR(7 downto 0) := x"07";
begin
    process(clk, reset)
        variable fb : STD_LOGIC;
    begin
        if reset = '1' then
            r <= (others => '0');
        elsif rising_edge(clk) then
            -- BEGIN_EDIT
            fb := r(7) xor din;
            if fb = '1' then
                r <= (r(6 downto 0) & '0') xor POLY;
            else
                r <= r(6 downto 0) & '0';
            end if;
            -- END_EDIT
        end if;
    end process;

    crc <= r;
end architecture behavioral;
