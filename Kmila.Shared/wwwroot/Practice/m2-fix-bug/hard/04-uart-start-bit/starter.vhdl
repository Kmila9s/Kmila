library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity uart_start is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        rx    : in  STD_LOGIC;
        state : out STD_LOGIC_VECTOR(1 downto 0)
    );
end entity uart_start;

architecture behavioral of uart_start is
    -- BUG: BIT_PERIOD is wrong by half — the FSM samples mid-period
    -- and slides one position relative to the data bits.
    constant BIT_PERIOD : integer := 5;
    type state_t is (IDLE, START, DATA, STOP);
    signal s        : state_t := IDLE;
    signal cnt      : integer range 0 to 31 := 0;
begin
    process(clk, reset)
    begin
        if reset = '1' then
            s   <= IDLE;
            cnt <= 0;
        elsif rising_edge(clk) then
            case s is
                when IDLE =>
                    if rx = '0' then
                        s   <= START;
                        cnt <= 0;
                    end if;
                when START =>
                    if cnt = BIT_PERIOD - 1 then
                        s   <= DATA;
                        cnt <= 0;
                    else
                        cnt <= cnt + 1;
                    end if;
                when DATA =>
                    if cnt = BIT_PERIOD - 1 then
                        s   <= STOP;
                        cnt <= 0;
                    else
                        cnt <= cnt + 1;
                    end if;
                when STOP =>
                    s   <= IDLE;
                    cnt <= 0;
            end case;
        end if;
    end process;

    with s select
        state <= "00" when IDLE,
                 "01" when START,
                 "10" when DATA,
                 "11" when STOP;
end architecture behavioral;
