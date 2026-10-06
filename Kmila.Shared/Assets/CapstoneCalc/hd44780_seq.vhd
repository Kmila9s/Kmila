-- ============================================================================
-- hd44780_seq.vhd — HD44780 4-bit bus sequencer for the capstone LCD.
--
-- Kmila "real hardware by default" rework (2026-10-06).
--
-- Reads the 256-bit packed character buffer produced by lcd_driver (2 rows x
-- 16 chars, MSB-first: char 0 of line 0 in bits 255..248) and writes it to a
-- real HD44780 display over the 4-bit parallel bus, so the Visual LCD widget is
-- driven in raw mode instead of being handed ready-made text.
--
-- After reset it runs one full redraw:
--   Clear display (0x01) -> Set DDRAM line 0 (0x80) -> 16 data bytes ->
--   Set DDRAM line 1 (0xC0) -> 16 data bytes.
-- Each byte is sent HIGH nibble then LOW nibble on DB7..DB4, each nibble
-- latched by a rising edge on E (exactly two E pulses per byte, as the widget's
-- 4-bit decoder expects). RW is tied low (write only).
--
-- STEP_CLKS is sim-scaled so a full redraw completes within a short run; a real
-- board would hold each bus state for the HD44780's documented setup/hold time.
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;

entity hd44780_seq is
    generic (
        STEP_CLKS : positive := 2   -- clocks each micro-step is held
    );
    port (
        clk    : in  std_logic;
        rst_n  : in  std_logic;
        chars  : in  std_logic_vector(255 downto 0);  -- packed 2x16 ASCII
        lcd_rs : out std_logic;                        -- 0 = command, 1 = data
        lcd_rw : out std_logic;                        -- tied low (write)
        lcd_e  : out std_logic;                        -- enable strobe
        lcd_db : out std_logic_vector(7 downto 4)      -- 4-bit data bus
    );
end entity hd44780_seq;

architecture rtl of hd44780_seq is
    signal buf      : std_logic_vector(255 downto 0) := (others => '0');
    signal bcnt     : integer range 0 to 35 := 0;   -- 0..34 active, 35 = done
    signal phase    : integer range 0 to 3 := 0;
    signal div_cnt  : integer range 0 to STEP_CLKS := 0;
    signal cur_byte : std_logic_vector(7 downto 0) := (others => '0');
    signal rs_r     : std_logic := '0';
    signal e_r      : std_logic := '0';
    signal db_r     : std_logic_vector(7 downto 4) := "0000";
    signal loaded   : std_logic := '0';
begin
    process(clk, rst_n)
        variable b    : std_logic_vector(7 downto 0);
        variable rs_v : std_logic;
    begin
        if rst_n = '0' then
            buf      <= (others => '0');
            bcnt     <= 0;
            phase    <= 0;
            div_cnt  <= 0;
            cur_byte <= (others => '0');
            rs_r     <= '0';
            e_r      <= '0';
            db_r     <= "0000";
            loaded   <= '0';
        elsif rising_edge(clk) then
            if loaded = '0' then
                buf    <= chars;    -- latch the buffer once, then stream it out
                loaded <= '1';
            elsif bcnt <= 34 then
                -- Current byte + RS for this position in the redraw script.
                if bcnt = 0 then
                    rs_v := '0'; b := "00000001";      -- clear display
                elsif bcnt = 1 then
                    rs_v := '0'; b := "10000000";      -- set DDRAM addr 0x00 (line 0)
                elsif bcnt = 18 then
                    rs_v := '0'; b := "11000000";      -- set DDRAM addr 0x40 (line 1)
                else
                    rs_v := '1'; b := buf(255 downto 248);  -- a data byte
                end if;

                if div_cnt < STEP_CLKS - 1 then
                    div_cnt <= div_cnt + 1;
                else
                    div_cnt <= 0;
                    case phase is
                        when 0 =>                 -- setup HIGH nibble, E low
                            rs_r     <= rs_v;
                            cur_byte <= b;
                            db_r     <= b(7 downto 4);
                            e_r      <= '0';
                            phase    <= 1;
                        when 1 =>                 -- E rising -> latch high nibble
                            e_r   <= '1';
                            phase <= 2;
                        when 2 =>                 -- setup LOW nibble, E low
                            e_r   <= '0';
                            db_r  <= cur_byte(3 downto 0);
                            phase <= 3;
                        when others =>            -- E rising -> latch low nibble
                            e_r   <= '1';
                            phase <= 0;
                            -- Shift to the next char after a data byte.
                            if (bcnt >= 2 and bcnt <= 17) or (bcnt >= 19 and bcnt <= 34) then
                                buf <= buf(247 downto 0) & "00000000";
                            end if;
                            bcnt <= bcnt + 1;
                    end case;
                end if;
            else
                e_r <= '0';   -- redraw complete; hold the bus idle
            end if;
        end if;
    end process;

    lcd_rs <= rs_r;
    lcd_rw <= '0';
    lcd_e  <= e_r;
    lcd_db <= db_r;
end architecture rtl;
