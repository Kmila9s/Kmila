library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity fifo_wrap is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        push  : in  STD_LOGIC;
        pop   : in  STD_LOGIC;
        full  : out STD_LOGIC;
        empty : out STD_LOGIC
    );
end entity fifo_wrap;

architecture behavioral of fifo_wrap is
    constant DEPTH : integer := 4;
    -- BUG: pointers wrap at 5 instead of DEPTH (4). One slot is
    -- effectively dropped and `full` lies about the real fill level.
    signal w_ptr : unsigned(2 downto 0) := (others => '0');
    signal r_ptr : unsigned(2 downto 0) := (others => '0');
    signal count : unsigned(2 downto 0) := (others => '0');
begin
    process(clk, reset)
    begin
        if reset = '1' then
            w_ptr <= (others => '0');
            r_ptr <= (others => '0');
            count <= (others => '0');
        elsif rising_edge(clk) then
            if push = '1' and count < DEPTH then
                if w_ptr = 4 then        -- BUG: should be DEPTH-1 = 3
                    w_ptr <= (others => '0');
                else
                    w_ptr <= w_ptr + 1;
                end if;
                if pop = '0' or count = 0 then
                    count <= count + 1;
                end if;
            end if;
            if pop = '1' and count > 0 then
                if r_ptr = 4 then        -- BUG: should be DEPTH-1 = 3
                    r_ptr <= (others => '0');
                else
                    r_ptr <= r_ptr + 1;
                end if;
                if push = '0' or count = DEPTH then
                    count <= count - 1;
                end if;
            end if;
        end if;
    end process;

    full  <= '1' when count = DEPTH else '0';
    empty <= '1' when count = 0 else '0';
end architecture behavioral;
