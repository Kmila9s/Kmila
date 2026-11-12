namespace Kmila.Shared.Pages;

// Counter gallery examples (2026-09-23) — two "real" designs contributed by
// the user: a 0-9 single seven-segment counter and a 0-9999 four-digit
// multiplexed counter. Both use RAW pin wiring in their .kviz sidecars (no
// assisted mode) and exercise frequency dividers + (for 9999) time-multiplexed
// display scanning.
//
// CLK_FREQ is shipped as 50 (not the real 50_000_000) so the divider rolls at
// a seconds-ish scale the interpreter can reach quickly — a 50 MHz divider
// would need tens of millions of ticks per increment and never advances in an
// interactive sim. The real board value is noted in each file's header.
//
// Each design also ships a ready-to-run VHDL testbench (contador_*_tb.vhd) so
// Workbench mode has pre-filled stimulus out of the box.
public partial class ProjectsView
{
    // ---------------- Contador 0-9 (single 7-segment) ----------------

    private const string CONTADOR9_VHD =
"""
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

-- 0-9 counter driving one common-anode 7-segment display.
-- NOTE: CLK_FREQ is 50 here (not the board's real 50_000_000) so the
-- divider advances in seconds-scale ticks the simulator can reach quickly.
entity Contador_9 is
    Port (
        clk       : in  STD_LOGIC;
        reset     : in  STD_LOGIC;
        segmentos : out STD_LOGIC_VECTOR(6 downto 0)
    );
end Contador_9;

architecture Behavioral of Contador_9 is

    constant CLK_FREQ : integer := 50;               -- sim-scaled (real: 50_000_000)

    signal contador_clk : integer range 0 to CLK_FREQ - 1 := 0;
    signal numero       : integer range 0 to 9 := 0;

begin

    -- Frequency divider + 0-9 counter
    process(clk, reset)
    begin
        if reset = '1' then
            contador_clk <= 0;
            numero <= 0;
        elsif rising_edge(clk) then
            if contador_clk = CLK_FREQ - 1 then
                contador_clk <= 0;
                if numero = 9 then
                    numero <= 0;
                else
                    numero <= numero + 1;
                end if;
            else
                contador_clk <= contador_clk + 1;
            end if;
        end if;
    end process;

    -- 7-segment decoder. segmentos(6 downto 0) = abcdefg.
    -- Common anode: 0 = segment on, 1 = segment off.
    process(numero)
    begin
        case numero is
            when 0 => segmentos <= "0000001";
            when 1 => segmentos <= "1001111";
            when 2 => segmentos <= "0010010";
            when 3 => segmentos <= "0000110";
            when 4 => segmentos <= "1001100";
            when 5 => segmentos <= "0100100";
            when 6 => segmentos <= "0100000";
            when 7 => segmentos <= "0001111";
            when 8 => segmentos <= "0000000";
            when 9 => segmentos <= "0000100";
            when others => segmentos <= "1111111";
        end case;
    end process;

end Behavioral;
""";

    // Raw wiring: each segment pin bound to a bit of `segmentos` (MSB=a=bit6),
    // common-anode polarity (segment lit when the bit reads '0'). A reset
    // button drives the reset input; the clock is auto-injected on the canvas.
    private const string CONTADOR9_KVIZ =
"""
{
  "version": 1,
  "designFile": "contador_9.vhd",
  "canvas": { "width": 1200, "height": 650, "background": "grid" },
  "components": [
    { "id": "btn-reset", "type": "button", "position": { "x": 80, "y": 80 }, "config": { "label": "RESET" }, "bindings": { "out": "reset" } },
    { "id": "seg0", "type": "seven-seg", "position": { "x": 320, "y": 220 }, "config": { "mode": "raw", "polarity": "anode", "color": "red" },
      "bindings": { "a": "segmentos[6]", "b": "segmentos[5]", "c": "segmentos[4]", "d": "segmentos[3]", "e": "segmentos[2]", "f": "segmentos[1]", "g": "segmentos[0]" } }
  ],
  "settings": {}
}
""";

    private const string CONTADOR9_TB =
"""
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- Testbench for Contador_9. Generates a 50 MHz-style clock (20 ns period),
-- pulses reset, then lets the counter free-run. Suggested run duration: ~12 us
-- (the sim-scaled divider increments the digit roughly every 1 us).
entity Contador_9_tb is
end Contador_9_tb;

architecture sim of Contador_9_tb is
    signal clk       : STD_LOGIC := '0';
    signal reset     : STD_LOGIC := '0';
    signal segmentos : STD_LOGIC_VECTOR(6 downto 0);
begin

    dut : entity work.Contador_9
        port map (
            clk       => clk,
            reset     => reset,
            segmentos => segmentos
        );

    -- 20 ns clock period (50 MHz).
    clk_gen : process
    begin
        clk <= '0';
        wait for 10 ns;
        clk <= '1';
        wait for 10 ns;
    end process;

    -- Stimulus: one clean reset pulse, then observe.
    stim : process
    begin
        reset <= '1';
        wait for 45 ns;
        reset <= '0';
        report "Contador_9: reset released, counting begins";
        wait for 12 us;
        report "Contador_9: testbench finished";
        wait;
    end process;

end architecture sim;
""";

    // ---------------- Contador 0-9999 (4-digit multiplexed) ----------------

    private const string CONTADOR9999_VHD =
"""
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

-- 0-9999 counter on a 4-digit multiplexed common-anode 7-segment bank.
-- NOTE: CLK_FREQ (50) and MUX_FREQ (5) are sim-scaled so both the 1-second
-- increment and the display multiplexing are visible in an interactive run.
-- Real board values: CLK_FREQ = 50_000_000, MUX_FREQ ~= 50_000.
entity Contador_9999 is
    Port (
        clk       : in  STD_LOGIC;
        reset     : in  STD_LOGIC;
        segmentos : out STD_LOGIC_VECTOR(6 downto 0);
        anodos    : out STD_LOGIC_VECTOR(3 downto 0)
    );
end Contador_9999;

architecture Behavioral of Contador_9999 is

    constant CLK_FREQ   : integer := 50;             -- sim-scaled (real: 50_000_000)
    constant ONE_SECOND : integer := CLK_FREQ - 1;
    constant MUX_FREQ   : integer := 5;              -- sim-scaled (real: ~50_000)

    signal contador     : integer range 0 to 9999 := 0;
    signal contador_clk : integer range 0 to ONE_SECOND := 0;

    signal contador_mux : integer range 0 to MUX_FREQ - 1 := 0;
    signal selector     : integer range 0 to 3 := 0;

    signal unidad  : integer range 0 to 9 := 0;
    signal decena  : integer range 0 to 9 := 0;
    signal centena : integer range 0 to 9 := 0;
    signal millar  : integer range 0 to 9 := 0;

    signal digito_actual : integer range 0 to 9 := 0;

begin

    -- Main divider: +1 count per "second".
    process(clk, reset)
    begin
        if reset = '1' then
            contador_clk <= 0;
            contador <= 0;
        elsif rising_edge(clk) then
            if contador_clk = ONE_SECOND then
                contador_clk <= 0;
                if contador = 9999 then
                    contador <= 0;
                else
                    contador <= contador + 1;
                end if;
            else
                contador_clk <= contador_clk + 1;
            end if;
        end if;
    end process;

    -- Multiplex divider: cycle the active digit.
    process(clk, reset)
    begin
        if reset = '1' then
            contador_mux <= 0;
            selector <= 0;
        elsif rising_edge(clk) then
            if contador_mux = MUX_FREQ - 1 then
                contador_mux <= 0;
                if selector = 3 then
                    selector <= 0;
                else
                    selector <= selector + 1;
                end if;
            else
                contador_mux <= contador_mux + 1;
            end if;
        end if;
    end process;

    -- Split the number into 4 digits.
    unidad  <= contador mod 10;
    decena  <= (contador / 10) mod 10;
    centena <= (contador / 100) mod 10;
    millar  <= contador / 1000;

    -- Select the active display (anodos active-low).
    process(selector, unidad, decena, centena, millar)
    begin
        case selector is
            when 0 =>
                digito_actual <= unidad;
                anodos <= "1110";
            when 1 =>
                digito_actual <= decena;
                anodos <= "1101";
            when 2 =>
                digito_actual <= centena;
                anodos <= "1011";
            when others =>
                digito_actual <= millar;
                anodos <= "0111";
        end case;
    end process;

    -- BCD to 7-segment. segmentos(6 downto 0) = abcdefg, common anode.
    process(digito_actual)
    begin
        case digito_actual is
            when 0 => segmentos <= "0000001";
            when 1 => segmentos <= "1001111";
            when 2 => segmentos <= "0010010";
            when 3 => segmentos <= "0000110";
            when 4 => segmentos <= "1001100";
            when 5 => segmentos <= "0100100";
            when 6 => segmentos <= "0100000";
            when 7 => segmentos <= "0001111";
            when 8 => segmentos <= "0000000";
            when 9 => segmentos <= "0000100";
            when others => segmentos <= "1111111";
        end case;
    end process;

end Behavioral;
""";

    // Raw wiring: a multiplexed 7-segment bank. `seg` (7-bit, MSB=a) is the
    // shared segment bus, `sel` (4-bit) the digit-select; anode polarity +
    // active-low select match the common-anode design.
    private const string CONTADOR9999_KVIZ =
"""
{
  "version": 1,
  "designFile": "contador_9999.vhd",
  "canvas": { "width": 1200, "height": 650, "background": "grid" },
  "components": [
    { "id": "btn-reset", "type": "button", "position": { "x": 80, "y": 80 }, "config": { "label": "RESET" }, "bindings": { "out": "reset" } },
    { "id": "bank", "type": "seven-seg-bank", "position": { "x": 260, "y": 220 },
      "config": { "digits": 4, "mode": "mux", "wiring": "explicit", "polarity": "anode", "selectActiveLow": true, "color": "red" },
      "bindings": { "a": "segmentos[6]", "b": "segmentos[5]", "c": "segmentos[4]", "d": "segmentos[3]", "e": "segmentos[2]", "f": "segmentos[1]", "g": "segmentos[0]", "an0": "anodos[0]", "an1": "anodos[1]", "an2": "anodos[2]", "an3": "anodos[3]" } }
  ],
  "settings": {}
}
""";

    private const string CONTADOR9999_TB =
"""
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- Testbench for Contador_9999. Generates a 50 MHz-style clock (20 ns period),
-- pulses reset, then observes the multiplexed count. Suggested run duration:
-- ~20 us (enough to watch several increments and the digit scanning).
entity Contador_9999_tb is
end Contador_9999_tb;

architecture sim of Contador_9999_tb is
    signal clk       : STD_LOGIC := '0';
    signal reset     : STD_LOGIC := '0';
    signal segmentos : STD_LOGIC_VECTOR(6 downto 0);
    signal anodos    : STD_LOGIC_VECTOR(3 downto 0);
begin

    dut : entity work.Contador_9999
        port map (
            clk       => clk,
            reset     => reset,
            segmentos => segmentos,
            anodos    => anodos
        );

    -- 20 ns clock period (50 MHz).
    clk_gen : process
    begin
        clk <= '0';
        wait for 10 ns;
        clk <= '1';
        wait for 10 ns;
    end process;

    -- Stimulus: one clean reset pulse, then observe.
    stim : process
    begin
        reset <= '1';
        wait for 45 ns;
        reset <= '0';
        report "Contador_9999: reset released, counting begins";
        wait for 20 us;
        report "Contador_9999: testbench finished";
        wait;
    end process;

end architecture sim;
""";

    // Testbench file for each counter, injected via ExampleDef.ExtraFiles so
    // Workbench mode is pre-filled. Kept at project root (not tb/) because the
    // examples pipeline writes ExtraFiles at the project root.
    private static IEnumerable<(string FileName, string Content)> Contador9ExtraFiles()
    {
        yield return ("contador_9_tb.vhd", CONTADOR9_TB);
    }

    private static IEnumerable<(string FileName, string Content)> Contador9999ExtraFiles()
    {
        yield return ("contador_9999_tb.vhd", CONTADOR9999_TB);
    }
}
