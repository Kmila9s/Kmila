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
    -- Round-robin pointer: index of the requester that "had its turn"
    -- last. Next grant scans starting from pointer+1 and wraps around.
    signal last_grant : unsigned(1 downto 0) := (others => '0');
    signal grant_int  : STD_LOGIC_VECTOR(3 downto 0) := (others => '0');
begin
    process(clk, reset)
        variable idx     : integer range 0 to 3;
        variable found   : boolean;
        variable scan    : integer range 0 to 3;
    begin
        if reset = '1' then
            last_grant <= (others => '0');
            grant_int  <= (others => '0');
        elsif rising_edge(clk) then
            grant_int <= (others => '0');
            found := false;
            for k in 1 to 4 loop
                scan := (to_integer(last_grant) + k) mod 4;
                if (not found) and req(scan) = '1' then
                    grant_int(scan) <= '1';
                    last_grant      <= to_unsigned(scan, 2);
                    found := true;
                end if;
            end loop;
        end if;
    end process;

    grant <= grant_int;
end architecture behavioral;
