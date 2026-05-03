library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity pwm_comp_fill is
    port (
        clk     : in  STD_LOGIC;
        reset   : in  STD_LOGIC;
        duty    : in  STD_LOGIC_VECTOR(3 downto 0);
        pwm_out : out STD_LOGIC
    );
end entity pwm_comp_fill;

architecture behavioral of pwm_comp_fill is
    signal count : unsigned(3 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            count <= (others => '0');
        elsif rising_edge(clk) then
            count <= count + 1;
        end if;
    end process;

    -- BEGIN_EDIT
    pwm_out <= '1' when count < unsigned(duty) else '0';
    -- END_EDIT
end architecture behavioral;
