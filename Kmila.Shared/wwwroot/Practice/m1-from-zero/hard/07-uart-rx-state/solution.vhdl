library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity uart_rx_state is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        rx    : in  STD_LOGIC;
        state : out STD_LOGIC_VECTOR(1 downto 0)
    );
end entity uart_rx_state;

architecture behavioral of uart_rx_state is
    signal s : STD_LOGIC_VECTOR(1 downto 0) := "00";
begin
    process(clk, reset)
    begin
        if reset = '1' then
            s <= "00";
        elsif rising_edge(clk) then
            case s is
                when "00" =>
                    if rx = '0' then
                        s <= "01";
                    end if;
                when "01" =>
                    s <= "10";
                when "10" =>
                    s <= "11";
                when others =>
                    s <= "00";
            end case;
        end if;
    end process;

    state <= s;
end architecture behavioral;
