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
            if s = "00" then
                s <= "01";
            elsif s = "01" then
                s <= "10";
            else
                s <= "00";
            end if;
        end if;
    end process;
    state <= std_logic_vector(s);
end architecture behavioral;
