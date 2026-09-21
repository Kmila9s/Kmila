namespace Kmila.Shared.Services.VisualRun;

// Public catalog of Visual-mode widgets — declares each widget's pin
// surface and its overall I/O direction relative to the design under
// simulation. Was previously duplicated (privately) inside
// VisualWiringModal.razor; extracted so VisualPanel can show a
// Direction chip in the props pane and sanitize stale bindings on load
// (R3-5, 2026-07-04).

public enum PinDir
{
    // Widget-side direction — In means the widget consumes a signal
    // FROM the design (LED, 7-seg, stepper: design outputs light them
    // up). Out means the widget drives a signal INTO the design
    // (button, switch: user action feeds the FPGA input port).
    In,
    Out,
}

public enum WidgetDirection
{
    Input,   // widget produces stimulus into the design (button, switch)
    Output,  // widget visualizes design outputs (LED, LED-array, 7-seg, stepper)
}

public readonly record struct KvizPin(string Name, PinDir Direction);

public static class KvizWidgetCatalog
{
    public static IEnumerable<KvizPin> PinsOf(KvizComponent c) => c.Type switch
    {
        "led"       => new[] { new KvizPin("anode",  PinDir.In) },
        "button"    => new[] { new KvizPin("out",    PinDir.Out) },
        "switch"    => new[] { new KvizPin("out",    PinDir.Out) },
        "led-array" => new[] { new KvizPin("anodes", PinDir.In) },
        "stepper"   => new[]
        {
            new KvizPin("phaseA",  PinDir.In),
            new KvizPin("phaseB",  PinDir.In),
            new KvizPin("phaseAn", PinDir.In),
            new KvizPin("phaseBn", PinDir.In),
        },
        "seven-seg"    => SevenSegPins(c.Config.GetString("mode", "raw")),
        "seven-seg-bank" => SevenSegBankPins(
                                c.Config.GetString("mode", "raw"),
                                c.Config.GetString("wiring", "packed"),
                                c.Config.GetInt("digits", 4)),
        "multi-switch" => MultiSwitchPins(
                              c.Config.GetString("mode", "per-bit"),
                              c.Config.GetInt("count", 4)),
        "matrix-3x3"   => LedMatrix33Pins(c.Config.GetString("mode", "value")),
        "matrix-3x3-rgb" => RgbMatrix33Pins(c.Config.GetString("mode", "planes")),
        "buzzer-piezo" => BuzzerPins(c.Config.GetString("mode", "freq")),
        "keypad-matrix" => KeypadPins(
            c.Config.GetString("mode", "decoded"),
            c.Config.GetInt("rows", 4),
            c.Config.GetInt("cols", 4)),
        "lcd-hd44780" => LcdPins(
            c.Config.GetString("mode", "text"),
            c.Config.GetInt("rows", 2),
            c.Config.GetInt("cols", 16)),
        _ => Array.Empty<KvizPin>(),
    };

    private static IEnumerable<KvizPin> SevenSegPins(string mode) => mode == "raw"
        ? new[]
        {
            new KvizPin("a",  PinDir.In),
            new KvizPin("b",  PinDir.In),
            new KvizPin("c",  PinDir.In),
            new KvizPin("d",  PinDir.In),
            new KvizPin("e",  PinDir.In),
            new KvizPin("f",  PinDir.In),
            new KvizPin("g",  PinDir.In),
            new KvizPin("dp", PinDir.In),
        }
        : new[]
        {
            new KvizPin("value", PinDir.In),
            new KvizPin("dp",    PinDir.In),
        };

    // Multi-switch pin surface — per-bit exposes N discrete outputs (real
    // DIP switch), vector collapses to a single N-bit port so the design
    // sees std_logic_vector(count-1 downto 0). Count is clamped to 2..8
    // to match the physical parts Daniel referenced (SPST-2/4/8 banks).
    private static IEnumerable<KvizPin> MultiSwitchPins(string mode, int count)
    {
        var n = Math.Clamp(count, 2, 8);
        if (mode == "vector")
        {
            return new[] { new KvizPin("value", PinDir.Out) };
        }
        var pins = new KvizPin[n];
        for (int i = 0; i < n; i++) pins[i] = new KvizPin($"out{i}", PinDir.Out);
        return pins;
    }

    // 3×3 mono LED matrix pin surface. Raw exposes both anode+cathode per
    // cell so students can wire row/col drive as on a real board (18 pins);
    // value mode collapses to a 9-bit LSB=row0col0 row-major vector so the
    // design can drive the grid with a single std_logic_vector(8 downto 0).
    private static IEnumerable<KvizPin> LedMatrix33Pins(string mode)
    {
        if (mode == "raw")
        {
            var pins = new KvizPin[18];
            int idx = 0;
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    pins[idx++] = new KvizPin($"a{r}{c}", PinDir.In);
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    pins[idx++] = new KvizPin($"k{r}{c}", PinDir.In);
            return pins;
        }
        return new[] { new KvizPin("value", PinDir.In) };
    }

    // 3×3 RGB LED matrix pin surface. `planes` mode collapses the surface
    // to three 9-bit vectors (`red`, `green`, `blue`) so the design drives
    // each colour plane with a single std_logic_vector(8 downto 0). Raw
    // exposes all 27 per-cell colour anodes `{r|g|b}{r}{c}`, matching a
    // physical common-cathode RGB matrix wired one LED-per-colour-per-cell.
    // Compact common-cathode-per-row wiring is a later variant per plan.
    private static IEnumerable<KvizPin> RgbMatrix33Pins(string mode)
    {
        if (mode == "raw")
        {
            var pins = new KvizPin[27];
            int idx = 0;
            foreach (var ch in new[] { 'r', 'g', 'b' })
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 3; c++)
                        pins[idx++] = new KvizPin($"{ch}{r}{c}", PinDir.In);
            return pins;
        }
        return new[]
        {
            new KvizPin("red",   PinDir.In),
            new KvizPin("green", PinDir.In),
            new KvizPin("blue",  PinDir.In),
        };
    }

    // Piezo buzzer pin surface. Raw exposes the single `signal` pin
    // (student implements the freq divider themselves); assisted `freq`
    // exposes a 16-bit unsigned `freq_hz` port + a `enable` gate. The
    // widget parses freq_hz each frame and retunes the Web Audio
    // oscillator when enable = '1'.
    private static IEnumerable<KvizPin> BuzzerPins(string mode)
    {
        if (mode == "signal") return new[] { new KvizPin("signal", PinDir.In) };
        return new[]
        {
            new KvizPin("freq_hz", PinDir.In),
            new KvizPin("enable",  PinDir.In),
        };
    }

    // Matrix keypad pin surface. Decoded mode (default) collapses the
    // matrix scan into two widget outputs: `key` (BCD-ish 4-bit code of
    // the lowest pressed key) + `pressed` strobe. Raw-scan exposes the
    // full row-in / col-out matrix so students can implement the scan
    // themselves — rows are widget inputs (design drives them low to
    // scan), cols are widget outputs (widget grounds a col when a key
    // on the currently-scanned row is pressed). First widget with a
    // MIXED pin-direction surface — props pane renders two chips via
    // PinCountsByDir instead of the single-direction chip other
    // widgets use.
    private static IEnumerable<KvizPin> KeypadPins(string mode, int rows, int cols)
    {
        rows = Math.Clamp(rows, 3, 4);
        cols = Math.Clamp(cols, 3, 4);
        if (mode == "raw-scan")
        {
            var pins = new List<KvizPin>(rows + cols);
            for (int r = 0; r < rows; r++)
                pins.Add(new KvizPin($"row{r}", PinDir.In));   // widget reads row scans from design
            for (int c = 0; c < cols; c++)
                pins.Add(new KvizPin($"col{c}", PinDir.Out));  // widget writes col back to design
            return pins;
        }
        return new[]
        {
            new KvizPin("key",     PinDir.Out),
            new KvizPin("pressed", PinDir.Out),
        };
    }

    // HD44780 LCD pin surface. Three modes:
    //   • "text" (assisted, default) — one `line{N}` port per row,
    //     `std_logic_vector((cols*8)-1 downto 0)` carrying ASCII bytes
    //     MSB-first (char 0 in the top bits). Optional `backlight`.
    //   • "raw-4bit" — full HD44780 control bus with 4-bit data:
    //     `RS`, `RW`, `E`, `DB4..DB7`. Student implements the init
    //     sequence + DDRAM writes exactly like a real board.
    //   • "raw-8bit" — same but with `DB0..DB7` (full 8-bit data bus).
    // Rows clamped 2..4; cols clamped 16..20 (2×16 / 2×20 / 4×16 /
    // 4×20 — the four HD44780 variants shipped in real kits).
    private static IEnumerable<KvizPin> LcdPins(string mode, int rows, int cols)
    {
        rows = Math.Clamp(rows, 2, 4);
        cols = Math.Clamp(cols, 16, 20);
        if (mode == "raw-4bit" || mode == "raw-8bit")
        {
            var pins = new List<KvizPin>
            {
                new KvizPin("RS", PinDir.In),
                new KvizPin("RW", PinDir.In),
                new KvizPin("E",  PinDir.In),
            };
            int lo = (mode == "raw-8bit") ? 0 : 4;
            for (int i = lo; i <= 7; i++)
                pins.Add(new KvizPin($"DB{i}", PinDir.In));
            return pins;
        }
        // text mode
        var textPins = new List<KvizPin>(rows + 1);
        for (int r = 0; r < rows; r++)
            textPins.Add(new KvizPin($"line{r}", PinDir.In));
        textPins.Add(new KvizPin("backlight", PinDir.In));
        return textPins;
    }

    // 7-segment bank pin surface. Three "independent" decode modes keep the
    // original single packed `value` bus (one dedicated slice per digit); the
    // "mux" mode models a real multiplexed display — shared segment lines +
    // one per-digit common/select (anode/cathode) line. In mux, the `wiring`
    // config picks how those lines are exposed:
    //   • "packed"   — two vectors: `seg` (7-bit, MSB=a) + `sel` (digits-bit,
    //                  MSB-first so char 0 = leftmost digit N-1). Compact.
    //   • "explicit" — individual pins: `a..g` + `an0..an{digits-1}`, each
    //                  separately bindable (verbose but precise).
    // All pins are In (the design drives both segments and selects).
    private static IEnumerable<KvizPin> SevenSegBankPins(string mode, string wiring, int digits)
    {
        if (mode != "mux")
            return new[] { new KvizPin("value", PinDir.In) };

        var n = Math.Clamp(digits, 2, 8);
        if (wiring == "explicit")
        {
            var pins = new List<KvizPin>(7 + n);
            foreach (var seg in new[] { "a", "b", "c", "d", "e", "f", "g" })
                pins.Add(new KvizPin(seg, PinDir.In));
            for (int d = 0; d < n; d++)
                pins.Add(new KvizPin($"an{d}", PinDir.In));
            return pins;
        }
        // packed
        return new[]
        {
            new KvizPin("seg", PinDir.In),
            new KvizPin("sel", PinDir.In),
        };
    }

    // Widgets are usually purely-input or purely-output, but the keypad
    // in raw-scan mode has both. Callers use this to render one chip per
    // direction (Inputs: N / Outputs: M) instead of the single
    // DirectionOf tag.
    public static (int InCount, int OutCount) PinCountsByDir(KvizComponent c)
    {
        int inC = 0, outC = 0;
        foreach (var p in PinsOf(c))
        {
            if (p.Direction == PinDir.In) inC++;
            else outC++;
        }
        return (inC, outC);
    }

    public static WidgetDirection? DirectionOf(string widgetType) => widgetType switch
    {
        "button" or "switch" or "multi-switch"                                                                                                 => WidgetDirection.Input,
        "led" or "led-array" or "seven-seg" or "seven-seg-bank" or "stepper" or "matrix-3x3" or "matrix-3x3-rgb" or "buzzer-piezo" or "lcd-hd44780" => WidgetDirection.Output,
        // keypad-matrix intentionally omitted: raw-scan mixes directions.
        // Panels should call PinCountsByDir to render two chips.
        _                                                                                                                                       => null,
    };

    // Drop bindings whose pin key isn't valid for the widget's current
    // type + config. Catches stale entries after mode changes (7-seg
    // raw→hex loses a..g), corrupt hand-edited .kviz files, and any
    // future refactor where a pin gets renamed. Returns the number of
    // bindings dropped so the caller can log a translation-pending
    // notice if desired.
    public static int Sanitize(KvizDoc doc)
    {
        if (doc?.Components == null) return 0;
        int dropped = 0;
        foreach (var c in doc.Components)
        {
            var valid = PinsOf(c).Select(p => p.Name).ToHashSet();
            var stale = c.Bindings.Keys.Where(k => !valid.Contains(k)).ToArray();
            foreach (var k in stale)
            {
                c.Bindings.Remove(k);
                dropped++;
            }
        }
        return dropped;
    }
}
