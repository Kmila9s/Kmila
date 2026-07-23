-- ============================================================================
-- ram.vhd — dual-port scratch RAM for the capstone calculator.
--
-- Kmila full audit 2026-07-08, Phase G, Ring 3.
--
-- • 32 words × 32 bits (q16_t values).
-- • Synchronous write on port A, asynchronous read on port B (typical FPGA
--   block-RAM inference pattern).
--
-- Usage in the calculator:
--   Addresses 0..15    → operand stack (token operand queue).
--   Addresses 16..23   → CPU scratch registers (working values, spill).
--   Addresses 24..31   → operator queue (op codes packed into low 5 bits).
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;
library work;
use work.types_pkg.all;

entity ram is
    generic (
        ADDR_W : positive := 5;    -- 2**5 = 32 words
        DATA_W : positive := 32
    );
    port (
        clk       : in  std_logic;
        rst_n     : in  std_logic;

        -- Write port
        we        : in  std_logic;
        wr_addr   : in  unsigned(ADDR_W-1 downto 0);
        wr_data   : in  q16_t;

        -- Read port
        rd_addr   : in  unsigned(ADDR_W-1 downto 0);
        rd_data   : out q16_t
    );
end entity ram;

architecture rtl of ram is
    type mem_t is array (0 to 2**ADDR_W - 1) of q16_t;
    signal mem : mem_t := (others => Q16_ZERO);
begin

    -- Synchronous write on rising edge, active reset clears the whole array.
    process (clk, rst_n)
    begin
        if rst_n = '0' then
            mem <= (others => Q16_ZERO);
        elsif rising_edge(clk) then
            if we = '1' then
                mem(to_integer(wr_addr)) <= wr_data;
            end if;
        end if;
    end process;

    -- Asynchronous read.
    rd_data <= mem(to_integer(rd_addr));

end architecture rtl;
