library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity fsm_next_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        x     : in  STD_LOGIC;
        state : out STD_LOGIC_VECTOR(1 downto 0)
    );
end entity fsm_next_fill;

architecture behavioral of fsm_next_fill is
    signal cur, nxt : STD_LOGIC_VECTOR(1 downto 0) := "00";
begin
    process(clk, reset)
    begin
        if reset = '1' then
            cur <= "00";
        elsif rising_edge(clk) then
            cur <= nxt;
        end if;
    end process;

    process(cur, x)
    begin
        case cur is
            when "00" =>
                if x = '1' then nxt <= "01"; else nxt <= "00"; end if;
            when "01" =>
                if x = '1' then nxt <= "10"; else nxt <= "00"; end if;
            when "10" =>
                if x = '1' then nxt <= "10"; else nxt <= "00"; end if;
            when others =>
                nxt <= "00";
        end case;
    end process;

    state <= cur;
end architecture behavioral;
