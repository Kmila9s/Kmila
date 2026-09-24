library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity lfsr_tap_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        q     : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity lfsr_tap_fill;

architecture behavioral of lfsr_tap_fill is
    signal r : STD_LOGIC_VECTOR(7 downto 0) := "11111111";
begin
    process(clk, reset)
        variable fb : STD_LOGIC;
    begin
        if reset = '1' then
            r <= "11111111";
        elsif rising_edge(clk) then
            fb := r(7) xor r(5) xor r(4) xor r(3);
            r <= r(6 downto 0) & fb;
        end if;
    end process;
    q <= r;
end architecture behavioral;
