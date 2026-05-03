library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity pwm is
    port (
        clk     : in  STD_LOGIC;
        reset   : in  STD_LOGIC;
        duty    : in  STD_LOGIC_VECTOR(3 downto 0);
        pwm_out : out STD_LOGIC
    );
end entity pwm;

architecture behavioral of pwm is
    signal cnt : unsigned(3 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            cnt <= (others => '0');
        elsif rising_edge(clk) then
            cnt <= cnt + 1;
        end if;
    end process;

    pwm_out <= '1' when cnt < unsigned(duty) else '0';
end architecture behavioral;
