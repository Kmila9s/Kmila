library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity gray_next_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        g     : out STD_LOGIC_VECTOR(2 downto 0)
    );
end entity gray_next_fill;

architecture behavioral of gray_next_fill is
    signal b  : unsigned(2 downto 0) := (others => '0');
    signal gi : STD_LOGIC_VECTOR(2 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            b <= (others => '0');
        elsif rising_edge(clk) then
            b <= b + 1;
        end if;
    end process;

    gi(2) <= b(2);
    gi(1) <= b(2) xor b(1);
    gi(0) <= b(1) xor b(0);

    g <= gi;
end architecture behavioral;
