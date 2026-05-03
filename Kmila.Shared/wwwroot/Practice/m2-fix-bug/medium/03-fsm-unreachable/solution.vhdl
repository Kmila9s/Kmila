library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity fsm_unreach is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        x     : in  STD_LOGIC;
        state : out STD_LOGIC_VECTOR(1 downto 0)
    );
end entity fsm_unreach;

architecture behavioral of fsm_unreach is
    type state_t is (S0, S1, S2);
    signal cur : state_t := S0;
begin
    process(clk, reset)
    begin
        if reset = '1' then
            cur <= S0;
        elsif rising_edge(clk) then
            case cur is
                when S0 =>
                    cur <= S1;
                when S1 =>
                    if x = '1' then
                        cur <= S2;
                    else
                        cur <= S1;
                    end if;
                when S2 =>
                    cur <= S0;
            end case;
        end if;
    end process;

    with cur select
        state <= "00" when S0,
                 "01" when S1,
                 "10" when S2;
end architecture behavioral;
