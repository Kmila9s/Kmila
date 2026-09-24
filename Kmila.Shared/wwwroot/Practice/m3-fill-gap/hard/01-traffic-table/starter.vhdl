library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity traffic_table_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        state : out STD_LOGIC_VECTOR(1 downto 0)
    );
end entity traffic_table_fill;

architecture behavioral of traffic_table_fill is
    signal cur  : STD_LOGIC_VECTOR(1 downto 0) := "00";
    signal nxt  : STD_LOGIC_VECTOR(1 downto 0);
begin
    process(clk, reset)
    begin
        if reset = '1' then
            cur <= "00";
        elsif rising_edge(clk) then
            cur <= nxt;
        end if;
    end process;

    process(cur)
    begin
        -- BEGIN_EDIT
        -- TODO: write the next-state CASE for the 3-state cycle
        --       00 -> 01 -> 10 -> 00 (others -> 00)
        case cur is
            when others => nxt <= "00";
        end case;
        -- END_EDIT
    end process;

    state <= cur;
end architecture behavioral;
