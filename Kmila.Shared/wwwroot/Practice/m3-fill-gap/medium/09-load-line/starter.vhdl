library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity load_branch_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        load  : in  STD_LOGIC;
        d     : in  STD_LOGIC_VECTOR(3 downto 0);
        q     : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity load_branch_fill;

architecture behavioral of load_branch_fill is
    signal qi : STD_LOGIC_VECTOR(3 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            qi <= (others => '0');
        elsif rising_edge(clk) then
            -- BEGIN_EDIT
            -- TODO: when load='1', capture d into qi (otherwise hold)
            if load = '0' then
                qi <= qi;
            end if;
            -- END_EDIT
        end if;
    end process;
    q <= qi;
end architecture behavioral;
