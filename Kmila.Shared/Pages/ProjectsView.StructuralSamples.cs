namespace Kmila.Shared.Pages;

// Multi-component (structural / port map) gallery examples (2026-10-06).
//
// These are the app's first samples built from several linked VHDL files where a
// top entity instantiates smaller ones via `port map` — the modular, "build a
// big block from small blocks" style. Kmila's engine flattens the hierarchy
// (Interpreter SourceElaborator) so they simulate like any single-file design.
//
// Authoring rules honoured for every sample (SourceElaborator caveats):
//   • One architecture per entity.
//   • `library ieee; use ieee.std_logic_1164.all;` lives in the ENTRY file
//     (the top, or the testbench) — child files rely on it being in scope.
//   • No generics, so the simple `label : entity work.X port map (...)` direct-
//     instantiation form is used throughout.
//   • Each sample ships a testbench (via ExtraFiles) that drives the inputs so
//     the Waveform viewer shows the truth table; the .kviz wires switches to the
//     inputs and LEDs to the outputs for the Visual tab.
public partial class ProjectsView
{
    // ===================================================================
    // 1. Half-adders -> full adder by components
    // ===================================================================

    private const string HALF_ADDER_VHD = """
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- Half adder: the smallest building block. Adds two single bits and produces a
-- sum (a XOR b) and a carry (a AND b). No carry-in.
entity half_adder is
    port(
        a     : in  std_logic;
        b     : in  std_logic;
        sum   : out std_logic;
        carry : out std_logic
    );
end half_adder;

architecture rtl of half_adder is
begin
    sum   <= a xor b;
    carry <= a and b;
end rtl;
""";

    private const string FULL_ADDER_VHD = """
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- Full adder built STRUCTURALLY from two half adders plus an OR gate.
--   * ha1 adds a + b           -> partial sum s1, carry c1
--   * ha2 adds s1 + cin        -> final sum,      carry c2
--   * cout is c1 OR c2 (a carry out of either stage)
-- This is the classic "a full adder is two half adders" composition, wired with
-- port map. full_adder is the top entity for this example.
entity full_adder is
    port(
        a    : in  std_logic;
        b    : in  std_logic;
        cin  : in  std_logic;
        sum  : out std_logic;
        cout : out std_logic
    );
end full_adder;

architecture structural of full_adder is
    signal s1 : std_logic;   -- sum out of the first half adder
    signal c1 : std_logic;   -- carry out of the first half adder
    signal c2 : std_logic;   -- carry out of the second half adder
begin
    ha1 : entity work.half_adder
        port map (a => a, b => b, sum => s1, carry => c1);

    ha2 : entity work.half_adder
        port map (a => s1, b => cin, sum => sum, carry => c2);

    cout <= c1 or c2;
end structural;
""";

    private const string FULL_ADDER_TB = """
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- Walks the full adder through all 8 input combinations so the waveform shows
-- the complete truth table.
entity full_adder_tb is
end full_adder_tb;

architecture sim of full_adder_tb is
    signal a, b, cin, sum, cout : std_logic := '0';
begin
    dut : entity work.full_adder
        port map (a => a, b => b, cin => cin, sum => sum, cout => cout);

    stim : process
    begin
        for i in 0 to 7 loop
            a   <= '1' when (i / 4) mod 2 = 1 else '0';
            b   <= '1' when (i / 2) mod 2 = 1 else '0';
            cin <= '1' when i mod 2 = 1 else '0';
            wait for 50 ns;
        end loop;
        report "full_adder: testbench finished";
        wait;
    end process;
end architecture sim;
""";

    private const string FULL_ADDER_KVIZ = """
{
  "version": 1,
  "designFile": "full_adder.vhd",
  "canvas": { "width": 900, "height": 500, "background": "grid" },
  "components": [
    { "id": "sw-a",   "type": "switch", "position": { "x": 60,  "y": 60  }, "config": { "label": "A" },   "bindings": { "out": "a" } },
    { "id": "sw-b",   "type": "switch", "position": { "x": 60,  "y": 180 }, "config": { "label": "B" },   "bindings": { "out": "b" } },
    { "id": "sw-cin", "type": "switch", "position": { "x": 60,  "y": 300 }, "config": { "label": "CIN" }, "bindings": { "out": "cin" } },
    { "id": "led-sum",  "type": "led", "position": { "x": 360, "y": 120 }, "config": { "color": "green" }, "bindings": { "anode": "sum" } },
    { "id": "led-cout", "type": "led", "position": { "x": 360, "y": 260 }, "config": { "color": "red" },   "bindings": { "anode": "cout" } }
  ],
  "settings": { "ringBufferTicks": 2000, "defaultSpeed": "Normal" }
}
""";

    private static IEnumerable<(string FileName, string Content)> FullAdderExtraFiles()
    {
        yield return ("half_adder.vhd", HALF_ADDER_VHD);
        yield return ("full_adder_tb.vhd", FULL_ADDER_TB);
    }

    // ===================================================================
    // 2. 4-bit ripple-carry adder (chain of full adders)
    // ===================================================================

    private const string RIPPLE4_VHD = """
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- 4-bit ripple-carry adder: four full adders chained so each stage's carry-out
-- feeds the next stage's carry-in. Reuses the same full_adder (itself two half
-- adders) from the half-adder example, so this is a three-level hierarchy:
--   ripple_adder4 -> full_adder -> half_adder.
-- ripple_adder4 is the top entity.
entity ripple_adder4 is
    port(
        a    : in  std_logic_vector(3 downto 0);
        b    : in  std_logic_vector(3 downto 0);
        cin  : in  std_logic;
        sum  : out std_logic_vector(3 downto 0);
        cout : out std_logic
    );
end ripple_adder4;

architecture structural of ripple_adder4 is
    signal c : std_logic_vector(4 downto 0);  -- carry chain; c(0)=cin, c(4)=cout
begin
    c(0) <= cin;

    fa0 : entity work.full_adder
        port map (a => a(0), b => b(0), cin => c(0), sum => sum(0), cout => c(1));
    fa1 : entity work.full_adder
        port map (a => a(1), b => b(1), cin => c(1), sum => sum(1), cout => c(2));
    fa2 : entity work.full_adder
        port map (a => a(2), b => b(2), cin => c(2), sum => sum(2), cout => c(3));
    fa3 : entity work.full_adder
        port map (a => a(3), b => b(3), cin => c(3), sum => sum(3), cout => c(4));

    cout <= c(4);
end structural;
""";

    private const string RIPPLE4_TB = """
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- Drives a few operand pairs through the 4-bit adder, including a case that
-- overflows into the carry-out.
entity ripple_adder4_tb is
end ripple_adder4_tb;

architecture sim of ripple_adder4_tb is
    signal a, b, sum : std_logic_vector(3 downto 0) := "0000";
    signal cin, cout : std_logic := '0';
begin
    dut : entity work.ripple_adder4
        port map (a => a, b => b, cin => cin, sum => sum, cout => cout);

    stim : process
    begin
        a <= "0011"; b <= "0100"; cin <= '0';   -- 3 + 4 = 7
        wait for 80 ns;
        a <= "1001"; b <= "0110"; cin <= '0';   -- 9 + 6 = 15
        wait for 80 ns;
        a <= "1111"; b <= "0001"; cin <= '0';   -- 15 + 1 = 16 (carry out)
        wait for 80 ns;
        a <= "0101"; b <= "0101"; cin <= '1';   -- 5 + 5 + 1 = 11
        wait for 80 ns;
        report "ripple_adder4: testbench finished";
        wait;
    end process;
end architecture sim;
""";

    private const string RIPPLE4_KVIZ = """
{
  "version": 1,
  "designFile": "ripple_adder4.vhd",
  "canvas": { "width": 1100, "height": 560, "background": "grid" },
  "components": [
    { "id": "dip-a", "type": "multi-switch", "position": { "x": 60, "y": 80 }, "config": { "mode": "per-bit", "count": 4 },
      "bindings": { "out0": "a[0]", "out1": "a[1]", "out2": "a[2]", "out3": "a[3]" } },
    { "id": "dip-b", "type": "multi-switch", "position": { "x": 60, "y": 220 }, "config": { "mode": "per-bit", "count": 4 },
      "bindings": { "out0": "b[0]", "out1": "b[1]", "out2": "b[2]", "out3": "b[3]" } },
    { "id": "sw-cin", "type": "switch", "position": { "x": 60, "y": 360 }, "config": { "label": "CIN" }, "bindings": { "out": "cin" } },
    { "id": "sum-arr", "type": "led-array", "position": { "x": 400, "y": 160 }, "config": { "length": 4, "color": "green", "orientation": "h" }, "bindings": { "anodes": "sum" } },
    { "id": "led-cout", "type": "led", "position": { "x": 400, "y": 300 }, "config": { "color": "red" }, "bindings": { "anode": "cout" } }
  ],
  "settings": { "ringBufferTicks": 2000, "defaultSpeed": "Normal" }
}
""";

    private static IEnumerable<(string FileName, string Content)> Ripple4ExtraFiles()
    {
        yield return ("half_adder.vhd", HALF_ADDER_VHD);
        yield return ("full_adder.vhd", FULL_ADDER_VHD);
        yield return ("ripple_adder4_tb.vhd", RIPPLE4_TB);
    }

    // ===================================================================
    // 3. 4-to-1 multiplexer from 2-to-1 blocks
    // ===================================================================

    private const string MUX2_VHD = """
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- 2-to-1 multiplexer: picks d0 when sel = '0', d1 when sel = '1'.
entity mux2 is
    port(
        d0  : in  std_logic;
        d1  : in  std_logic;
        sel : in  std_logic;
        y   : out std_logic
    );
end mux2;

architecture rtl of mux2 is
begin
    y <= d0 when sel = '0' else d1;
end rtl;
""";

    private const string MUX4_VHD = """
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- 4-to-1 multiplexer built STRUCTURALLY from three 2-to-1 muxes:
--   * m0 selects between d0 and d1 using sel(0)
--   * m1 selects between d2 and d3 using sel(0)
--   * m2 selects between those two results using sel(1)
-- mux4 is the top entity.
entity mux4 is
    port(
        d   : in  std_logic_vector(3 downto 0);
        sel : in  std_logic_vector(1 downto 0);
        y   : out std_logic
    );
end mux4;

architecture structural of mux4 is
    signal lo : std_logic;   -- d0/d1 chosen by sel(0)
    signal hi : std_logic;   -- d2/d3 chosen by sel(0)
begin
    m0 : entity work.mux2
        port map (d0 => d(0), d1 => d(1), sel => sel(0), y => lo);
    m1 : entity work.mux2
        port map (d0 => d(2), d1 => d(3), sel => sel(0), y => hi);
    m2 : entity work.mux2
        port map (d0 => lo, d1 => hi, sel => sel(1), y => y);
end structural;
""";

    private const string MUX4_TB = """
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- Holds a fixed data pattern on d and sweeps sel through 0..3 so the waveform
-- shows y following the selected input.
entity mux4_tb is
end mux4_tb;

architecture sim of mux4_tb is
    signal d   : std_logic_vector(3 downto 0) := "1010";  -- d3 d2 d1 d0 = 1 0 1 0
    signal sel : std_logic_vector(1 downto 0) := "00";
    signal y   : std_logic;
begin
    dut : entity work.mux4
        port map (d => d, sel => sel, y => y);

    stim : process
    begin
        sel <= "00"; wait for 60 ns;   -- y = d0 = 0
        sel <= "01"; wait for 60 ns;   -- y = d1 = 1
        sel <= "10"; wait for 60 ns;   -- y = d2 = 0
        sel <= "11"; wait for 60 ns;   -- y = d3 = 1
        report "mux4: testbench finished";
        wait;
    end process;
end architecture sim;
""";

    private const string MUX4_KVIZ = """
{
  "version": 1,
  "designFile": "mux4.vhd",
  "canvas": { "width": 1000, "height": 520, "background": "grid" },
  "components": [
    { "id": "dip-d", "type": "multi-switch", "position": { "x": 60, "y": 80 }, "config": { "mode": "per-bit", "count": 4 },
      "bindings": { "out0": "d[0]", "out1": "d[1]", "out2": "d[2]", "out3": "d[3]" } },
    { "id": "dip-sel", "type": "multi-switch", "position": { "x": 60, "y": 240 }, "config": { "mode": "per-bit", "count": 2 },
      "bindings": { "out0": "sel[0]", "out1": "sel[1]" } },
    { "id": "led-y", "type": "led", "position": { "x": 420, "y": 160 }, "config": { "color": "green" }, "bindings": { "anode": "y" } }
  ],
  "settings": { "ringBufferTicks": 2000, "defaultSpeed": "Normal" }
}
""";

    private static IEnumerable<(string FileName, string Content)> Mux4ExtraFiles()
    {
        yield return ("mux2.vhd", MUX2_VHD);
        yield return ("mux4_tb.vhd", MUX4_TB);
    }

    // ===================================================================
    // 4. 4-bit magnitude comparator from 1-bit cascade slices
    // ===================================================================

    private const string CMP1_VHD = """
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- 1-bit comparator slice for a cascaded magnitude comparator. Each slice takes
-- the result of the more-significant bits (gt_in/eq_in/lt_in) and refines it
-- with this bit:
--   * if higher bits already decided (gt_in or lt_in), pass it through;
--   * otherwise (still equal so far) compare a vs b at this bit.
entity cmp1 is
    port(
        a      : in  std_logic;
        b      : in  std_logic;
        gt_in  : in  std_logic;
        eq_in  : in  std_logic;
        lt_in  : in  std_logic;
        gt_out : out std_logic;
        eq_out : out std_logic;
        lt_out : out std_logic
    );
end cmp1;

architecture rtl of cmp1 is
begin
    gt_out <= gt_in or (eq_in and a and (not b));
    lt_out <= lt_in or (eq_in and (not a) and b);
    eq_out <= eq_in and (not (a xor b));
end rtl;
""";

    private const string CMP4_VHD = """
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- 4-bit magnitude comparator built STRUCTURALLY from four 1-bit slices chained
-- from the most-significant bit down. The chain starts "equal" (eq=1) and each
-- slice narrows the verdict. The final slice's outputs say whether A > B,
-- A = B, or A < B. comparator4 is the top entity.
entity comparator4 is
    port(
        a     : in  std_logic_vector(3 downto 0);
        b     : in  std_logic_vector(3 downto 0);
        a_gt_b : out std_logic;
        a_eq_b : out std_logic;
        a_lt_b : out std_logic
    );
end comparator4;

architecture structural of comparator4 is
    -- Stage boundaries. Index 4 is the seed (nothing compared yet = equal);
    -- index 0 is the final verdict after all four bits.
    signal g : std_logic_vector(4 downto 0);
    signal e : std_logic_vector(4 downto 0);
    signal l : std_logic_vector(4 downto 0);
begin
    g(4) <= '0';
    e(4) <= '1';
    l(4) <= '0';

    c3 : entity work.cmp1
        port map (a => a(3), b => b(3), gt_in => g(4), eq_in => e(4), lt_in => l(4),
                  gt_out => g(3), eq_out => e(3), lt_out => l(3));
    c2 : entity work.cmp1
        port map (a => a(2), b => b(2), gt_in => g(3), eq_in => e(3), lt_in => l(3),
                  gt_out => g(2), eq_out => e(2), lt_out => l(2));
    c1 : entity work.cmp1
        port map (a => a(1), b => b(1), gt_in => g(2), eq_in => e(2), lt_in => l(2),
                  gt_out => g(1), eq_out => e(1), lt_out => l(1));
    c0 : entity work.cmp1
        port map (a => a(0), b => b(0), gt_in => g(1), eq_in => e(1), lt_in => l(1),
                  gt_out => g(0), eq_out => e(0), lt_out => l(0));

    a_gt_b <= g(0);
    a_eq_b <= e(0);
    a_lt_b <= l(0);
end structural;
""";

    private const string CMP4_TB = """
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

-- Compares a few operand pairs covering greater-than, equal, and less-than.
entity comparator4_tb is
end comparator4_tb;

architecture sim of comparator4_tb is
    signal a, b : std_logic_vector(3 downto 0) := "0000";
    signal a_gt_b, a_eq_b, a_lt_b : std_logic;
begin
    dut : entity work.comparator4
        port map (a => a, b => b, a_gt_b => a_gt_b, a_eq_b => a_eq_b, a_lt_b => a_lt_b);

    stim : process
    begin
        a <= "1001"; b <= "0110"; wait for 60 ns;   -- 9 > 6
        a <= "0101"; b <= "0101"; wait for 60 ns;   -- 5 = 5
        a <= "0011"; b <= "1100"; wait for 60 ns;   -- 3 < 12
        a <= "1111"; b <= "1110"; wait for 60 ns;   -- 15 > 14
        report "comparator4: testbench finished";
        wait;
    end process;
end architecture sim;
""";

    private const string CMP4_KVIZ = """
{
  "version": 1,
  "designFile": "comparator4.vhd",
  "canvas": { "width": 1100, "height": 540, "background": "grid" },
  "components": [
    { "id": "dip-a", "type": "multi-switch", "position": { "x": 60, "y": 80 }, "config": { "mode": "per-bit", "count": 4 },
      "bindings": { "out0": "a[0]", "out1": "a[1]", "out2": "a[2]", "out3": "a[3]" } },
    { "id": "dip-b", "type": "multi-switch", "position": { "x": 60, "y": 240 }, "config": { "mode": "per-bit", "count": 4 },
      "bindings": { "out0": "b[0]", "out1": "b[1]", "out2": "b[2]", "out3": "b[3]" } },
    { "id": "led-gt", "type": "led", "position": { "x": 420, "y": 100 }, "config": { "color": "green" },  "bindings": { "anode": "a_gt_b" } },
    { "id": "led-eq", "type": "led", "position": { "x": 420, "y": 220 }, "config": { "color": "yellow" }, "bindings": { "anode": "a_eq_b" } },
    { "id": "led-lt", "type": "led", "position": { "x": 420, "y": 340 }, "config": { "color": "red" },    "bindings": { "anode": "a_lt_b" } }
  ],
  "settings": { "ringBufferTicks": 2000, "defaultSpeed": "Normal" }
}
""";

    private static IEnumerable<(string FileName, string Content)> Comparator4ExtraFiles()
    {
        yield return ("cmp1.vhd", CMP1_VHD);
        yield return ("comparator4_tb.vhd", CMP4_TB);
    }
}
