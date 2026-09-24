library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity manchester_buggy is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        din   : in  STD_LOGIC;
        m     : out STD_LOGIC
    );
end entity manchester_buggy;

architecture behavioral of manchester_buggy is
    signal phase : STD_LOGIC := '0';
begin
    -- `phase` toggles every cycle, dividing each bit period into two
    -- halves. With IEEE 802.3 Manchester (G.E. Thomas convention):
    --   din = '0'  =>  m = "01"  (low then high)
    --   din = '1'  =>  m = "10"  (high then low)
    -- which collapses to:  m = din XOR phase.
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
