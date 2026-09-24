library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity manchester_enc is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        din   : in  STD_LOGIC;
        m     : out STD_LOGIC
    );
end entity manchester_enc;

architecture behavioral of manchester_enc is
    signal phase : STD_LOGIC := '0';
begin
    process(clk, reset)
    begin
        if reset = '1' then
            phase <= '0';
        elsif rising_edge(clk) then
            phase <= not phase;
        end if;
    end process;

    m <= din xor phase;
end architecture behavioral;
