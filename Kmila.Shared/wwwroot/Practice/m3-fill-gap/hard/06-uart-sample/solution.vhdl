library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity uart_sample_fill is
    port (
        clk        : in  STD_LOGIC;
        reset      : in  STD_LOGIC;
        bit_count  : in  STD_LOGIC_VECTOR(3 downto 0);
        sample_now : out STD_LOGIC
    );
end entity uart_sample_fill;

architecture behavioral of uart_sample_fill is
begin
    process(clk, reset)
    begin
        if reset = '1' then
            sample_now <= '0';
        elsif rising_edge(clk) then
            -- BEGIN_EDIT
            if unsigned(bit_count) = "1000" then
                sample_now <= '1';
            else
                sample_now <= '0';
            end if;
            -- END_EDIT
        end if;
    end process;
end architecture behavioral;
