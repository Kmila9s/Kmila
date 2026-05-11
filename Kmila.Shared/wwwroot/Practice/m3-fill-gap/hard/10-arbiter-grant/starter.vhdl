library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity arbiter_grant_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        req   : in  STD_LOGIC_VECTOR(3 downto 0);
        grant : out STD_LOGIC_VECTOR(3 downto 0)
    );
end entity arbiter_grant_fill;

architecture behavioral of arbiter_grant_fill is
    signal last_grant : unsigned(1 downto 0) := (others => '0');
    signal nxt_grant  : STD_LOGIC_VECTOR(3 downto 0) := (others => '0');
    signal cur_grant  : STD_LOGIC_VECTOR(3 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            last_grant <= (others => '0');
            cur_grant  <= (others => '0');
        elsif rising_edge(clk) then
            cur_grant <= nxt_grant;
            if    nxt_grant(0) = '1' then last_grant <= "00";
            elsif nxt_grant(1) = '1' then last_grant <= "01";
            elsif nxt_grant(2) = '1' then last_grant <= "10";
            elsif nxt_grant(3) = '1' then last_grant <= "11";
            else                          last_grant <= last_grant;
            end if;
        end if;
    end process;

    process(req, last_grant)
    begin
        -- BEGIN_EDIT
        nxt_grant <= (others => '0');  -- TODO: pick the next req bit after last_grant
        -- END_EDIT
    end process;

    grant <= cur_grant;
end architecture behavioral;
