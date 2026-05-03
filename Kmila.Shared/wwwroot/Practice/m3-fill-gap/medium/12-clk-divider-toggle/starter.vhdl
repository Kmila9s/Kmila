library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity divider_fill is
    port (
        clk     : in  STD_LOGIC;
        reset   : in  STD_LOGIC;
        clk_out : out STD_LOGIC
    );
end entity divider_fill;

architecture behavioral of divider_fill is
    constant TOP   : unsigned(3 downto 0) := to_unsigned(9, 4);
    signal count   : unsigned(3 downto 0) := (others => '0');
    signal clk_reg : STD_LOGIC := '0';
begin
    process(clk, reset)
    begin
        if reset = '1' then
            count   <= (others => '0');
            clk_reg <= '0';
        elsif rising_edge(clk) then
            if count = TOP then
                count <= (others => '0');
                -- BEGIN_EDIT
                -- TODO: toggle clk_reg when count reaches TOP
                clk_reg <= clk_reg;
                -- END_EDIT
            else
                count <= count + 1;
            end if;
        end if;
    end process;
    clk_out <= clk_reg;
end architecture behavioral;
