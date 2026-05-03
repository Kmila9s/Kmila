library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity shift_reg8 is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        din   : in  STD_LOGIC;
        q     : out STD_LOGIC_VECTOR(7 downto 0)
    );
end entity shift_reg8;

architecture behavioral of shift_reg8 is
    signal r : std_logic_vector(7 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            r <= (others => '0');
        elsif rising_edge(clk) then
            r <= r(6 downto 0) & din;
        end if;
    end process;
    q <= r;
end architecture behavioral;
