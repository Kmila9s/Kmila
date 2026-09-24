library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity crc_poly is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        din   : in  STD_LOGIC;
        crc   : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity crc_poly;

architecture behavioral of crc_poly is
    -- Polynomial 0x1D = x^8 + x^4 + x^3 + x^2 + 1.
    -- Taps below correspond to those XOR positions in the LFSR-style
    -- shifter (bit 0 driven by feedback, taps where coefficient is 1).
    signal reg : STD_LOGIC_VECTOR(7 downto 0) := (others => '0');
    signal fb  : STD_LOGIC;
begin
    fb <= reg(7) xor din;
    process(clk, reset)
    begin
        if reset = '1' then
            reg <= (others => '0');
        elsif rising_edge(clk) then
            reg(0) <= fb;
            reg(1) <= reg(0);
            reg(2) <= reg(1) xor fb;
            reg(3) <= reg(2) xor fb;
            reg(4) <= reg(3) xor fb;
            reg(5) <= reg(4);
            reg(6) <= reg(5);
            reg(7) <= reg(6);
        end if;
    end process;

    crc <= reg;
end architecture behavioral;
