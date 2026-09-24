library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity tff_noen is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        t     : in  STD_LOGIC;
        q     : out STD_LOGIC
    );
end entity tff_noen;

architecture behavioral of tff_noen is
    signal q_int : STD_LOGIC := '0';
begin
    process(clk, reset)
    begin
        if reset = '1' then
            q_int <= '0';
        elsif rising_edge(clk) then
            if t = '1' then
                q_int <= not q_int;
            end if;
        end if;
    end process;
    q <= q_int;
end architecture behavioral;
