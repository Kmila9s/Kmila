library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity arbiter_starve is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        req   : in  STD_LOGIC_VECTOR(3 downto 0);
        grant : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity arbiter_starve;

architecture behavioral of arbiter_starve is
    signal grant_int : STD_LOGIC_VECTOR(3 downto 0) := (others => '0');
begin
    -- BUG: fixed-priority encoder — bit 0 always wins. While req(0)
    -- stays high, req(3) (and 2, 1) are starved forever.
    process(clk, reset)
    begin
        if reset = '1' then
            grant_int <= (others => '0');
        elsif rising_edge(clk) then
            grant_int <= (others => '0');
            if    req(0) = '1' then grant_int(0) <= '1';
            elsif req(1) = '1' then grant_int(1) <= '1';
            elsif req(2) = '1' then grant_int(2) <= '1';
            elsif req(3) = '1' then grant_int(3) <= '1';
            end if;
        end if;
    end process;

    grant <= grant_int;
end architecture behavioral;
