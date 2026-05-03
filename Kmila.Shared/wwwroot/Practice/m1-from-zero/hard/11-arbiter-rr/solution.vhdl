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
        variable idx   : unsigned(1 downto 0);
        variable found : boolean;
        variable next_g: STD_LOGIC_VECTOR(3 downto 0);
    begin
        if reset = '1' then
            last <= (others => '0');
            g    <= (others => '0');
        elsif rising_edge(clk) then
            next_g := (others => '0');
            found  := false;
            idx    := last + 1;
            for i in 0 to 3 loop
                if not found then
                    if req(to_integer(idx)) = '1' then
                        next_g(to_integer(idx)) := '1';
                        last  <= idx;
                        found := true;
                    end if;
                    idx := idx + 1;
                end if;
            end loop;
            g <= next_g;
        end if;
    end process;

    grant <= g;
end architecture behavioral;
