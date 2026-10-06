using System.Reflection;

namespace Kmila.Shared.Pages;

// Ring 5 (2026-07-14) capstone gallery example — the CPU calculator design
// authored during the full audit (Rings 1-4). 15 VHDL files ship as
// embedded resources under Assets/CapstoneCalc/*.vhd (registered in
// Kmila.Shared.csproj) and load at pick time via GetManifestResourceStream.
//
// Inlining the ~70 KB VHDL corpus as C# string constants would bloat
// ProjectsView.razor without buying anything — resources keep the file
// lean while matching how the other examples ship (all-in-project).

/// <summary>Projects gallery page; this partial supplies the capstone calculator example's embedded VHDL corpus.</summary>
public partial class ProjectsView
{
    // File order matters for the Editor's file list. `calc_top.vhd` is
    // the top-level entity so it lands first (users see it selected by
    // default when they open the project). `testbench.vhd` sits at the
    // bottom because it's the driver, not part of the DUT hierarchy.
    private static readonly string[] CAPSTONE_CALC_FILES = new[]
    {
        "calc_top.vhd",
        "types_pkg.vhd",
        "fixed_point.vhd",
        "clock_divider.vhd",
        "ram.vhd",
        "rom.vhd",
        "alu.vhd",
        "expr_engine.vhd",
        "keypad_scanner.vhd",
        "lcd_driver.vhd",
        "hd44780_seq.vhd",
        "seg7_driver.vhd",
        "led_marquee.vhd",
        "buzzer_ctrl.vhd",
        "cpu.vhd",
        "testbench.vhd",
        "testbench_full.vhd",
    };

    private const string CAPSTONE_CALC_MAIN = "calc_top.vhd";

    // Load every capstone .vhd from the embedded resource pool, in the
    // order declared above. Any file that fails to resolve is skipped
    // (should be impossible given the resource registration, but the
    // gallery pick shouldn't crash if someone rebuilds without the
    // Assets folder in place).
    private static IEnumerable<(string FileName, string Content)> LoadCapstoneCalcFiles()
    {
        var asm = Assembly.GetExecutingAssembly();
        // Manifest resource names are constructed by the SDK as
        // "<RootNamespace>.<PathWithDotsForSlashes>". Kmila.Shared's root
        // namespace is `Kmila.Shared` so the pattern is
        // `Kmila.Shared.Assets.CapstoneCalc.<file>.vhd`.
        const string prefix = "Kmila.Shared.Assets.CapstoneCalc.";
        foreach (var name in CAPSTONE_CALC_FILES)
        {
            var resource = prefix + name;
            using var stream = asm.GetManifestResourceStream(resource);
            if (stream is null) continue;
            using var reader = new StreamReader(stream);
            yield return (name, reader.ReadToEnd());
        }
    }

    // Pre-wired .kviz sidecar for the capstone. Five widgets bound to
    // calc_top's external ports:
    //   • keypad-matrix (raw-scan 4×4)   → cols_in / rows_out (fan-in write)
    //   • lcd-hd44780   (text 2×16)      → lcd_chars[255:128] / [127:0]
    //   • seven-seg-bank (digits=4)      → seg_bus (28-bit)
    //   • matrix-3x3    (value mode)     → led_matrix (9-bit)
    //   • buzzer-piezo  (signal mode)    → buzzer_pin
    //
    // Bit-slice bindings exercise the Ring-5 fan-in path
    // (VisualRunCoordinator.SetSlicedInput + SliceHelper.Compose) and
    // the natural-split UX in VisualWiringModal.
    private const string CAPSTONE_CALC_KVIZ = """
{
  "version": 1,
  "designFile": "calc_top.vhd",
  "canvas": { "width": 1400, "height": 900, "background": "grid" },
  "components": [
    {
      "id": "keypad-1",
      "type": "keypad-matrix",
      "position": { "x": 60, "y": 460 },
      "config": {
        "mode": "raw-scan",
        "rows": 4,
        "cols": 4
      },
      "bindings": {
        "row0": "rows_out[0]",
        "row1": "rows_out[1]",
        "row2": "rows_out[2]",
        "row3": "rows_out[3]",
        "col0": "cols_in[0]",
        "col1": "cols_in[1]",
        "col2": "cols_in[2]",
        "col3": "cols_in[3]"
      }
    },
    {
      "id": "lcd-1",
      "type": "lcd-hd44780",
      "position": { "x": 380, "y": 60 },
      "config": {
        "mode": "raw-4bit",
        "rows": 2,
        "cols": 16
      },
      "bindings": {
        "RS": "lcd_rs",
        "RW": "lcd_rw",
        "E": "lcd_e",
        "DB4": "lcd_db[4]",
        "DB5": "lcd_db[5]",
        "DB6": "lcd_db[6]",
        "DB7": "lcd_db[7]"
      }
    },
    {
      "id": "segbank-1",
      "type": "seven-seg-bank",
      "position": { "x": 380, "y": 320 },
      "config": {
        "digits": 4,
        "color": "red"
      },
      "bindings": {
        "value": "seg_bus"
      }
    },
    {
      "id": "matrix-1",
      "type": "matrix-3x3",
      "position": { "x": 940, "y": 60 },
      "config": {
        "mode": "raw",
        "color": "red",
        "brightness": 100
      },
      "bindings": {
        "a00": "led_matrix[0]", "a01": "led_matrix[1]", "a02": "led_matrix[2]",
        "a10": "led_matrix[3]", "a11": "led_matrix[4]", "a12": "led_matrix[5]",
        "a20": "led_matrix[6]", "a21": "led_matrix[7]", "a22": "led_matrix[8]"
      }
    },
    {
      "id": "buzzer-1",
      "type": "buzzer-piezo",
      "position": { "x": 940, "y": 320 },
      "config": {
        "mode": "signal"
      },
      "bindings": {
        "signal": "buzzer_pin"
      }
    }
  ],
  "settings": {
    "ringBufferTicks": 2000,
    "defaultSpeed": "Normal"
  }
}
""";
}
