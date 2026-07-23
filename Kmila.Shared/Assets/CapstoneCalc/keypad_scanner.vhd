-- ============================================================================
-- keypad_scanner.vhd — 4×4 matrix keypad scanner for KeypadWidget.
--
-- Kmila full audit 2026-07-08, Phase G, Ring 2.
--
-- Cycles the 4 row outputs `rows` one-hot low, samples `cols` (active-low),
-- debounces the observed key for `DEBOUNCE_TICKS` scan ticks, then emits a
-- 4-bit `keycode` + single-cycle `key_strobe`.
--
-- Encoded per types_pkg.KEY_* (positional):
--   Row 0: 1 2 3 A
--   Row 1: 4 5 6 B
--   Row 2: 7 8 9 C
--   Row 3: * 0 # D
--
-- No key held / multiple keys → strobe stays low; last held key is retained
-- in keycode until a *different* single key is stable, then strobe pulses.
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;
library work;
use work.types_pkg.all;

entity keypad_scanner is
    generic (
        DEBOUNCE_TICKS : positive := 3   -- consecutive-stable scan ticks
    );
    port (
        clk        : in  std_logic;
        rst_n      : in  std_logic;
        scan_tick  : in  std_logic;      -- ~1 kHz strobe from clock_divider

        cols       : in  std_logic_vector(3 downto 0);   -- active-low column read-back
        rows       : out std_logic_vector(3 downto 0);   -- one-hot low
        keycode    : out keycode_t;
        key_strobe : out std_logic
    );
end entity keypad_scanner;

architecture rtl of keypad_scanner is
    -- Position → keycode table. Row-major, matching the physical layout above.
    type key_lut_t is array (0 to 15) of keycode_t;
    constant KEY_LUT : key_lut_t := (
        0  => KEY_1, 1  => KEY_2, 2  => KEY_3, 3  => KEY_A,
        4  => KEY_4, 5  => KEY_5, 6  => KEY_6, 7  => KEY_B,
        8  => KEY_7, 9  => KEY_8, 10 => KEY_9, 11 => KEY_C,
        12 => KEY_STAR, 13 => KEY_0, 14 => KEY_HASH, 15 => KEY_D
    );

    -- Idle counter: consecutive scan-ticks with no col grounded. After
    -- IDLE_RELEASE_TICKS of idle we clear prev_code so a re-press of the
    -- same key strobes again. 12 = 3 full row-cycles at 4 rows/cycle,
    -- matching the DEBOUNCE_TICKS=3 press-detect threshold.
    constant IDLE_RELEASE_TICKS : natural := 12;

    signal row_idx    : unsigned(1 downto 0) := (others => '0');
    signal rows_int   : std_logic_vector(3 downto 0) := (others => '1');
    signal last_pos   : integer range 0 to 16 := 16;   -- 16 = "no key"
    signal stable_pos : integer range 0 to 16 := 16;
    signal stable_cnt : unsigned(3 downto 0) := (others => '0');
    signal idle_cnt   : unsigned(4 downto 0) := (others => '0');
    signal prev_code  : keycode_t := (others => '0');
    signal strobe_int : std_logic := '0';
    signal code_int   : keycode_t := (others => '0');
begin

    -- Row driver: one-hot low. Active row is `row_idx`.
    process (row_idx)
    begin
        rows_int <= (others => '1');
        rows_int(to_integer(row_idx)) <= '0';
    end process;
    rows <= rows_int;

    -- Scan + debounce (Ring 5 fix 2026-07-14).
    --
    -- Bug in original: row_idx advanced every scan_tick, so `sensed`
    -- cycled through {press_pos, 16, 16, 16, press_pos, …} as the row
    -- driver walked through the matrix. Comparing `sensed` to
    -- `last_pos` on consecutive scan_ticks never saw 3 equal non-16
    -- values → no key ever debounced. Only "no key" (16) could ever
    -- reach DEBOUNCE_TICKS.
    --
    -- Fix: ignore idle samples (sensed=16) for debounce-tracking and
    -- count `press_pos` sightings across full row-cycles. Row_idx
    -- still advances every scan_tick so the widget's per-row col
    -- read-back stays correct. A separate idle counter clears
    -- `prev_code` after IDLE_RELEASE_TICKS consecutive no-key ticks
    -- so repeated presses of the same key re-strobe.
    process (clk, rst_n)
        variable sensed  : integer range 0 to 16;
        variable one_low : integer range 0 to 4;
        variable col_idx : integer range 0 to 3;
    begin
        if rst_n = '0' then
            row_idx    <= (others => '0');
            last_pos   <= 16;
            stable_pos <= 16;
            stable_cnt <= (others => '0');
            idle_cnt   <= (others => '0');
            prev_code  <= (others => '0');
            strobe_int <= '0';
            code_int   <= (others => '0');
        elsif rising_edge(clk) then
            strobe_int <= '0';

            if scan_tick = '1' then
                -- Sense: which column is low (if exactly one).
                one_low := 0;
                col_idx := 0;
                for i in 0 to 3 loop
                    if cols(i) = '0' then
                        one_low := one_low + 1;
                        col_idx := i;
                    end if;
                end loop;

                if one_low = 1 then
                    sensed := to_integer(row_idx) * 4 + col_idx;
                else
                    sensed := 16;
                end if;

                if sensed /= 16 then
                    idle_cnt <= (others => '0');
                    if sensed = last_pos then
                        if stable_cnt < to_unsigned(DEBOUNCE_TICKS, 4) then
                            stable_cnt <= stable_cnt + 1;
                        end if;
                        if stable_cnt = to_unsigned(DEBOUNCE_TICKS - 1, 4) then
                            stable_pos <= sensed;
                            if KEY_LUT(sensed) /= prev_code then
                                code_int   <= KEY_LUT(sensed);
                                strobe_int <= '1';
                                prev_code  <= KEY_LUT(sensed);
                            end if;
                        end if;
                    else
                        stable_cnt <= (others => '0');
                        last_pos   <= sensed;
                    end if;
                else
                    -- No key this row-tick. Skip debounce tracking so a
                    -- held key's "wrong-row" samples don't reset the
                    -- press counter. Clear prev_code only after a full
                    -- release window elapses.
                    if idle_cnt < to_unsigned(IDLE_RELEASE_TICKS, 5) then
                        idle_cnt <= idle_cnt + 1;
                    else
                        prev_code  <= (others => '0');
                        last_pos   <= 16;
                        stable_cnt <= (others => '0');
                        stable_pos <= 16;
                    end if;
                end if;

                -- Advance row scan every scan_tick — widget needs it to
                -- read col back correctly per row.
                row_idx <= row_idx + 1;
            end if;
        end if;
    end process;

    keycode    <= code_int;
    key_strobe <= strobe_int;

end architecture rtl;
