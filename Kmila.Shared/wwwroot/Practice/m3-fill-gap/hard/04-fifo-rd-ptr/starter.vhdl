library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity fifo_rd_fill is
    port (
        clk   : in  STD_LOGIC;
        reset : in  STD_LOGIC;
        push  : in  STD_LOGIC;
        pop   : in  STD_LOGIC;
        empty : out STD_LOGIC
    );
end entity fifo_rd_fill;

architecture behavioral of fifo_rd_fill is
    signal wr_ptr : unsigned(3 downto 0) := (others => '0');
    signal rd_ptr : unsigned(3 downto 0) := (others => '0');
    signal is_empty : STD_LOGIC;
begin
    is_empty <= '1' when wr_ptr = rd_ptr else '0';

    process(clk, reset)
    begin
        if reset = '1' then
            wr_ptr <= (others => '0');
            rd_ptr <= (others => '0');
        elsif rising_edge(clk) then
            if push = '1' then
                wr_ptr <= wr_ptr + 1;
            end if;
            -- BEGIN_EDIT
            -- TODO: increment rd_ptr ONLY when pop = '1' AND the FIFO is
            -- not empty (use is_empty = '0').
            -- (Common first-attempt stub — forgets the empty guard, so
            -- rd_ptr races past wr_ptr on the first pop against an empty
            -- FIFO and `empty` starts reporting stale values.)
            if pop = '1' then
                rd_ptr <= rd_ptr + 1;
            end if;
            -- END_EDIT
        end if;
    end process;

    empty <= is_empty;
end architecture behavioral;
