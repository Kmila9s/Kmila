library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity arbiter_rr is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        req   : in  STD_LOGIC_VECTOR(3 downto 0);
        grant : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity arbiter_rr;

architecture behavioral of arbiter_rr is
    signal last : unsigned(1 downto 0) := (others => '0');
    signal g    : STD_LOGIC_VECTOR(3 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            last <= (others => '0');
            g    <= (others => '0');
        elsif rising_edge(clk) then
            -- TODO: starting at last+1, scan req in a 4-iteration loop and grant the first set bit (one-hot).
            null;
        end if;
    end process;

    grant <= g;
end architecture behavioral;
