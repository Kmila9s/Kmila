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
    process(clk, reset)
    begin
        if reset = '1' then
            phase <= '0';
        elsif rising_edge(clk) then
            phase <= not phase;
        end if;
    end process;

    -- BUG: encoding inverted. With NOT (din XOR phase):
    --   din = '0'  =>  m = "10"
    --   din = '1'  =>  m = "01"
    -- This is the IEEE 802.3 convention's *opposite* (G.E. Thomas).
    m <= not (din xor phase);
end architecture behavioral;
