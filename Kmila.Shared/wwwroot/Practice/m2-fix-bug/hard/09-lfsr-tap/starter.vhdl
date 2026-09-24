library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity lfsr_tap is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        q     : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity lfsr_tap;

architecture behavioral of lfsr_tap is
    signal reg : STD_LOGIC_VECTOR(7 downto 0) := "00000001";
    signal fb  : STD_LOGIC;
begin
    -- BUG: only taps 8 and 7 are XOR'd. The polynomial is no longer
    -- primitive; the LFSR enters a short cycle and never visits all
    -- 255 non-zero states.
    fb <= reg(7) xor reg(6);

    process(clk, reset)
    begin
        if reset = '1' then
            reg <= "00000001";
        elsif rising_edge(clk) then
            reg <= reg(6 downto 0) & fb;
        end if;
    end process;

    q <= reg;
end architecture behavioral;
