namespace Kmila.Shared.Pages;

// Ready-to-run VHDL testbenches for the converted pure-VHDL gallery examples
// (2026-10-06). Each catalog example that was rebuilt as raw VHDL ships its
// own `*_tb.vhd` via ExampleDef.ExtraFiles so the Waveform viewer has pre-filled
// stimulus out of the box (Workbench / Testbench stimulus mode) — mirroring the
// contador-9 / contador-9999 reference in ProjectsView.Counters.cs.
//
// Convention (same as the counters):
//   • 20 ns clock period (50 MHz-style) from a `clk_gen` process.
//   • One clean reset pulse (`reset <= '1'; wait 45 ns; reset <= '0';`).
//   • For designs with real data inputs, the `stim` process also drives those
//     inputs so the trace shows meaningful transitions; designs with no data
//     input (only clk+reset) rely on the reset pulse as the whole stimulus,
//     exactly like the counters.
//   • Dividers in the designs are sim-scaled (small constants, real board value
//     noted in a header comment) so the trace advances within a short run.
public partial class ProjectsView
{
    // ---------------- Demo board ----------------
    private const string DEMO_BOARD_TB =
"""
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- Testbench for the Demo Board. 20 ns clock, one reset pulse, then RUN high so
-- the counter/heartbeat/stepper advance and the raw 7-segment decoder walks
-- through every digit. NUDGE is pulsed once so btn_echo blinks in the trace.
-- Suggested run: ~6 us.
entity demo_board_tb is
end demo_board_tb;

architecture sim of demo_board_tb is
    signal clk       : STD_LOGIC := '0';
    signal reset     : STD_LOGIC := '0';
    signal run       : STD_LOGIC := '0';
    signal dir       : STD_LOGIC := '1';
    signal nudge     : STD_LOGIC := '0';
    signal heartbeat : STD_LOGIC;
    signal count     : STD_LOGIC_VECTOR(3 downto 0);
    signal segments  : STD_LOGIC_VECTOR(6 downto 0);
    signal btn_echo  : STD_LOGIC;
    signal phase_a   : STD_LOGIC;
    signal phase_b   : STD_LOGIC;
    signal phase_an  : STD_LOGIC;
    signal phase_bn  : STD_LOGIC;
begin
    dut : entity work.demo_board
        port map (
            clk => clk, reset => reset, run => run, dir => dir, nudge => nudge,
            heartbeat => heartbeat, count => count, segments => segments,
            btn_echo => btn_echo,
            phase_a => phase_a, phase_b => phase_b,
            phase_an => phase_an, phase_bn => phase_bn
        );

    clk_gen : process
    begin
        clk <= '0'; wait for 10 ns;
        clk <= '1'; wait for 10 ns;
    end process;

    stim : process
    begin
        reset <= '1';
        wait for 45 ns;
        reset <= '0';
        run   <= '1';                 -- start counting
        wait for 2 us;
        nudge <= '1';                 -- single NUDGE pulse → btn_echo blinks
        wait for 200 ns;
        nudge <= '0';
        wait for 3 us;
        report "demo_board: testbench finished";
        wait;
    end process;
end architecture sim;
""";

    // ---------------- Counter clock (2-digit, raw 7-seg) ----------------
    private const string COUNTER_CLOCK_TB =
"""
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- Testbench for the 2-digit decimal counter. 20 ns clock, one reset pulse, then
-- TICK held high so the units digit advances every clock and rolls into tens.
-- The two 7-segment buses show the decoded digits. Suggested run: ~3 us.
entity counter_clock_tb is
end counter_clock_tb;

architecture sim of counter_clock_tb is
    signal clk       : STD_LOGIC := '0';
    signal reset     : STD_LOGIC := '0';
    signal tick      : STD_LOGIC := '0';
    signal units_seg : STD_LOGIC_VECTOR(6 downto 0);
    signal tens_seg  : STD_LOGIC_VECTOR(6 downto 0);
    signal overflow  : STD_LOGIC;
begin
    dut : entity work.counter_clock
        port map (
            clk => clk, reset => reset, tick => tick,
            units_seg => units_seg, tens_seg => tens_seg, overflow => overflow
        );

    clk_gen : process
    begin
        clk <= '0'; wait for 10 ns;
        clk <= '1'; wait for 10 ns;
    end process;

    stim : process
    begin
        reset <= '1';
        wait for 45 ns;
        reset <= '0';
        tick  <= '1';                 -- advance one count per clock
        wait for 3 us;
        report "counter_clock: testbench finished";
        wait;
    end process;
end architecture sim;
""";

    // ---------------- Traffic light FSM ----------------
    private const string TRAFFIC_LIGHT_TB =
"""
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- Testbench for the traffic-light FSM. 20 ns clock, one reset pulse, then
-- ENABLE high so the machine steps RED -> GREEN -> YELLOW every 10 clocks.
-- Suggested run: ~2 us (several full cycles).
entity traffic_light_tb is
end traffic_light_tb;

architecture sim of traffic_light_tb is
    signal clk       : STD_LOGIC := '0';
    signal reset     : STD_LOGIC := '0';
    signal enable    : STD_LOGIC := '0';
    signal red       : STD_LOGIC;
    signal yellow    : STD_LOGIC;
    signal green     : STD_LOGIC;
    signal state_out : STD_LOGIC_VECTOR(1 downto 0);
begin
    dut : entity work.traffic_light
        port map (
            clk => clk, reset => reset, enable => enable,
            red => red, yellow => yellow, green => green, state_out => state_out
        );

    clk_gen : process
    begin
        clk <= '0'; wait for 10 ns;
        clk <= '1'; wait for 10 ns;
    end process;

    stim : process
    begin
        reset  <= '1';
        wait for 45 ns;
        reset  <= '0';
        enable <= '1';                -- let the light cycle
        wait for 2 us;
        report "traffic_light: testbench finished";
        wait;
    end process;
end architecture sim;
""";

    // ---------------- Shift register ----------------
    private const string SHIFT_REGISTER_TB =
"""
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- Testbench for the 8-bit shift register. 20 ns clock, one reset pulse, then
-- SHIFT enabled. SI is driven high for a single clock to inject one '1' that
-- then walks left across reg_out on each subsequent clock. Suggested run: ~1 us.
entity shift_register_tb is
end shift_register_tb;

architecture sim of shift_register_tb is
    signal clk      : STD_LOGIC := '0';
    signal reset    : STD_LOGIC := '0';
    signal shift_en : STD_LOGIC := '0';
    signal si       : STD_LOGIC := '0';
    signal reg_out  : STD_LOGIC_VECTOR(7 downto 0);
begin
    dut : entity work.shift_register
        port map (
            clk => clk, reset => reset, shift_en => shift_en, si => si,
            reg_out => reg_out
        );

    clk_gen : process
    begin
        clk <= '0'; wait for 10 ns;
        clk <= '1'; wait for 10 ns;
    end process;

    stim : process
    begin
        reset    <= '1';
        wait for 45 ns;
        reset    <= '0';
        shift_en <= '1';
        si       <= '1';              -- inject one '1' ...
        wait for 20 ns;
        si       <= '0';              -- ... then watch it walk left
        wait for 1 us;
        report "shift_register: testbench finished";
        wait;
    end process;
end architecture sim;
""";

    // ---------------- DIP adder (combinational) ----------------
    private const string DIP_ADDER_TB =
"""
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

-- Testbench for the 4-bit DIP adder. The design is purely combinational (no
-- clock), so the stimulus walks the two operand buses through a few values and
-- the 5-bit sum (with carry-out in bit 4) follows immediately. Suggested run:
-- ~400 ns.
entity dip_adder_tb is
end dip_adder_tb;

architecture sim of dip_adder_tb is
    signal a   : STD_LOGIC_VECTOR(3 downto 0) := (others => '0');
    signal b   : STD_LOGIC_VECTOR(3 downto 0) := (others => '0');
    signal sum : STD_LOGIC_VECTOR(4 downto 0);
begin
    dut : entity work.dip_adder
        port map (a => a, b => b, sum => sum);

    stim : process
    begin
        a <= "0011"; b <= "0100";     -- 3 + 4 = 7
        wait for 100 ns;
        a <= "1001"; b <= "0110";     -- 9 + 6 = 15
        wait for 100 ns;
        a <= "1111"; b <= "0001";     -- 15 + 1 = 16 (carry out)
        wait for 100 ns;
        a <= "1010"; b <= "0101";     -- 10 + 5 = 15
        wait for 100 ns;
        report "dip_adder: testbench finished";
        wait;
    end process;
end architecture sim;
""";

    // ---------------- Buzzer scale (raw PWM) ----------------
    private const string BUZZER_SCALE_TB =
"""
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- Testbench for the raw buzzer scale generator. 20 ns clock, one reset pulse,
-- ENABLE high, then a couple of NOTE values so the PWM `signal` toggles at two
-- different rates (lower note count = faster toggle). Suggested run: ~3 us.
entity scale_generator_tb is
end scale_generator_tb;

architecture sim of scale_generator_tb is
    signal clk      : STD_LOGIC := '0';
    signal reset    : STD_LOGIC := '0';
    signal note_sel : STD_LOGIC_VECTOR(2 downto 0) := "000";
    signal enable   : STD_LOGIC := '0';
    signal signal_o : STD_LOGIC;
begin
    dut : entity work.scale_generator
        port map (clk => clk, reset => reset, note_sel => note_sel,
                  enable => enable, signal_o => signal_o);

    clk_gen : process
    begin
        clk <= '0'; wait for 10 ns;
        clk <= '1'; wait for 10 ns;
    end process;

    stim : process
    begin
        reset  <= '1';
        wait for 45 ns;
        reset  <= '0';
        enable <= '1';
        note_sel <= "000";            -- C4 (slow toggle)
        wait for 1500 ns;
        note_sel <= "111";            -- C5 (fast toggle)
        wait for 1500 ns;
        report "scale_generator: testbench finished";
        wait;
    end process;
end architecture sim;
""";

    // ---------------- Plus-sign scroller (raw 3x3 matrix) ----------------
    private const string MATRIX_PLUS_TB =
"""
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- Testbench for the plus-sign scroller. 20 ns clock, one reset pulse, then STEP
-- held high so the 9-bit pattern cycles through its six frames. Suggested run:
-- ~400 ns.
entity plus_sign_scroller_tb is
end plus_sign_scroller_tb;

architecture sim of plus_sign_scroller_tb is
    signal clk     : STD_LOGIC := '0';
    signal reset   : STD_LOGIC := '0';
    signal step_en : STD_LOGIC := '0';
    signal pattern : STD_LOGIC_VECTOR(8 downto 0);
begin
    dut : entity work.plus_sign_scroller
        port map (clk => clk, reset => reset, step_en => step_en, pattern => pattern);

    clk_gen : process
    begin
        clk <= '0'; wait for 10 ns;
        clk <= '1'; wait for 10 ns;
    end process;

    stim : process
    begin
        reset   <= '1';
        wait for 45 ns;
        reset   <= '0';
        step_en <= '1';
        wait for 400 ns;
        report "plus_sign_scroller: testbench finished";
        wait;
    end process;
end architecture sim;
""";

    // ---------------- RGB rainbow (raw 3x3 RGB matrix) ----------------
    private const string RGB_RAINBOW_TB =
"""
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- Testbench for the RGB rainbow. 20 ns clock, one reset pulse, then STEP held
-- high so the three LFSR planes advance and the red/green/blue buses change.
-- Suggested run: ~400 ns.
entity rgb_rainbow_tb is
end rgb_rainbow_tb;

architecture sim of rgb_rainbow_tb is
    signal clk     : STD_LOGIC := '0';
    signal reset   : STD_LOGIC := '0';
    signal step_en : STD_LOGIC := '0';
    signal red     : STD_LOGIC_VECTOR(8 downto 0);
    signal green   : STD_LOGIC_VECTOR(8 downto 0);
    signal blue    : STD_LOGIC_VECTOR(8 downto 0);
begin
    dut : entity work.rgb_rainbow
        port map (clk => clk, reset => reset, step_en => step_en,
                  red => red, green => green, blue => blue);

    clk_gen : process
    begin
        clk <= '0'; wait for 10 ns;
        clk <= '1'; wait for 10 ns;
    end process;

    stim : process
    begin
        reset   <= '1';
        wait for 45 ns;
        reset   <= '0';
        step_en <= '1';
        wait for 400 ns;
        report "rgb_rainbow: testbench finished";
        wait;
    end process;
end architecture sim;
""";

    // ExtraFiles loaders — each injects its testbench at the project root so
    // Workbench (Testbench) stimulus mode is pre-filled when the example opens.
    private static IEnumerable<(string FileName, string Content)> DemoBoardExtraFiles()
    {
        yield return ("demo_board_tb.vhd", DEMO_BOARD_TB);
    }

    private static IEnumerable<(string FileName, string Content)> CounterClockExtraFiles()
    {
        yield return ("counter_clock_tb.vhd", COUNTER_CLOCK_TB);
    }

    private static IEnumerable<(string FileName, string Content)> TrafficLightExtraFiles()
    {
        yield return ("traffic_light_tb.vhd", TRAFFIC_LIGHT_TB);
    }

    private static IEnumerable<(string FileName, string Content)> ShiftRegisterExtraFiles()
    {
        yield return ("shift_register_tb.vhd", SHIFT_REGISTER_TB);
    }

    private static IEnumerable<(string FileName, string Content)> DipAdderExtraFiles()
    {
        yield return ("dip_adder_tb.vhd", DIP_ADDER_TB);
    }

    private static IEnumerable<(string FileName, string Content)> BuzzerScaleExtraFiles()
    {
        yield return ("scale_generator_tb.vhd", BUZZER_SCALE_TB);
    }

    private static IEnumerable<(string FileName, string Content)> MatrixPlusExtraFiles()
    {
        yield return ("plus_sign_scroller_tb.vhd", MATRIX_PLUS_TB);
    }

    private static IEnumerable<(string FileName, string Content)> RgbRainbowExtraFiles()
    {
        yield return ("rgb_rainbow_tb.vhd", RGB_RAINBOW_TB);
    }
}
