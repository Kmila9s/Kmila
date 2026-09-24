library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity load_register is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        load  : in  STD_LOGIC;
        d     : in  STD_LOGIC_VECTOR(3 downto 0);
        q     : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity load_register;

architecture behavioral of load_register is
    signal r : std_logic_vector(3 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            r <= (others => '0');
        elsif rising_edge(clk) then
            if load = '1' then
                r <= d;
            end if;
        end if;
    end process;
    q <= r;
end architecture behavioral;
