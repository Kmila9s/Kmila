library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity handshake_race is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        req   : in  STD_LOGIC;
        ack   : out STD_LOGIC
    );
end entity handshake_race;

architecture behavioral of handshake_race is
    signal req_q1 : STD_LOGIC := '0';
    signal req_q2 : STD_LOGIC := '0';
begin
    -- Two-stage register: ack only asserts after req has been
    -- stable high for at least one full clock cycle.
    process(clk, reset)
    begin
        if reset = '1' then
            req_q1 <= '0';
            req_q2 <= '0';
        elsif rising_edge(clk) then
            req_q1 <= req;
            req_q2 <= req_q1;
        end if;
    end process;

    ack <= req_q1 and req_q2;
end architecture behavioral;
