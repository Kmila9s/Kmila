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
        nxt_grant <= (others => '0');
        case last_grant is
            when "00" =>
                if    req(1) = '1' then nxt_grant <= "0010";
                elsif req(2) = '1' then nxt_grant <= "0100";
                elsif req(3) = '1' then nxt_grant <= "1000";
                elsif req(0) = '1' then nxt_grant <= "0001";
                end if;
            when "01" =>
                if    req(2) = '1' then nxt_grant <= "0100";
                elsif req(3) = '1' then nxt_grant <= "1000";
                elsif req(0) = '1' then nxt_grant <= "0001";
                elsif req(1) = '1' then nxt_grant <= "0010";
                end if;
            when "10" =>
                if    req(3) = '1' then nxt_grant <= "1000";
                elsif req(0) = '1' then nxt_grant <= "0001";
                elsif req(1) = '1' then nxt_grant <= "0010";
                elsif req(2) = '1' then nxt_grant <= "0100";
                end if;
            when others =>
                if    req(0) = '1' then nxt_grant <= "0001";
                elsif req(1) = '1' then nxt_grant <= "0010";
                elsif req(2) = '1' then nxt_grant <= "0100";
                elsif req(3) = '1' then nxt_grant <= "1000";
                end if;
        end case;
    end process;

    grant <= cur_grant;
end architecture behavioral;
