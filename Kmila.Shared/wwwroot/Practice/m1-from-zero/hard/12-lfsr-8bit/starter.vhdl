library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity lfsr8 is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        q     : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity lfsr8;

architecture behavioral of lfsr8 is
    signal r : STD_LOGIC_VECTOR(7 downto 0) := (others => '1');
begin
    process(clk, reset)
        variable fb : STD_LOGIC;
    begin
        if reset = '1' then
            r <= (others => '1');
        elsif rising_edge(clk) then
            -- TODO: fb = r(7) xor r(5) xor r(4) xor r(3); shift left and insert fb.
            null;
        end if;
    end process;

    q <= r;
end architecture behavioral;
