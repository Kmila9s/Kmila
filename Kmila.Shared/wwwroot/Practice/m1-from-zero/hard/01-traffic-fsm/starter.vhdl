library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity traffic_fsm is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        state : out STD_LOGIC_VECTOR(1 downto 0)
    );
end entity traffic_fsm;

architecture behavioral of traffic_fsm is
    signal s : unsigned(1 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            s <= "00";
        elsif rising_edge(clk) then
            -- TODO: cycle through "00" -> "01" -> "10" -> "00".
            s <= s;
        end if;
    end process;
    state <= std_logic_vector(s);
end architecture behavioral;
