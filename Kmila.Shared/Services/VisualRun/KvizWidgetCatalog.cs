namespace Kmila.Shared.Services.VisualRun;

// Public catalog of Visual-mode widgets — declares each widget's pin
// surface and its overall I/O direction relative to the design under
// simulation. Was previously duplicated (privately) inside
// VisualWiringModal.razor; extracted so VisualPanel can show a
// Direction chip in the props pane and sanitize stale bindings on load
// (R3-5, 2026-07-04).

/// <summary>Direction of a single widget pin relative to the design under simulation.</summary>
public enum PinDir
{
    // Widget-side direction — In means the widget consumes a signal
    // FROM the design (LED, 7-seg, stepper: design outputs light them
    // up). Out means the widget drives a signal INTO the design
    // (button, switch: user action feeds the FPGA input port).
    /// <summary>The widget consumes a signal from the design (e.g. LED lit by a design output).</summary>
    In,
    /// <summary>The widget drives a signal into the design (e.g. button feeding an input port).</summary>
    Out,
}

/// <summary>Overall I/O role of a widget relative to the design under simulation.</summary>
public enum WidgetDirection
{
    /// <summary>Widget produces stimulus into the design (button, switch).</summary>
    Input,   // widget produces stimulus into the design (button, switch)
    /// <summary>Widget visualizes design outputs (LED, LED-array, 7-seg, stepper).</summary>
    Output,  // widget visualizes design outputs (LED, LED-array, 7-seg, stepper)
}

/// <summary>A named widget pin together with its direction.</summary>
/// <param name="Name">The pin name.</param>
/// <param name="Direction">The pin's direction relative to the design.</param>
public readonly record struct KvizPin(string Name, PinDir Direction);

/// <summary>Static catalog describing each Visual-mode widget's pins and I/O direction.</summary>
public static class KvizWidgetCatalog
{
    // ── Advanced-mode enforcement ────────────────────────────────────────
    // Each moded widget: (legacy default when `mode` is missing, raw
    // pin-level modes, assisted "Expert" modes). The legacy defaults are
    // kept for loading old .kviz files (and for Sanitize), but with
    // Advanced mode OFF an Expert/unknown mode is never honoured: the
    // widget is shown, wired and run in its first raw mode instead. The
    // stored `config.mode` is never rewritten, so turning Advanced back ON
    // restores the author's choice.
    private static readonly Dictionary<string, (string Default, string[] Raw, string[] Expert)> ModeSpec = new()
    {
        ["seven-seg"]      = ("raw",     new[] { "raw" },                             new[] { "hex", "dec" }),
        ["seven-seg-bank"] = ("raw",     new[] { "raw", "mux" },                      new[] { "hex", "dec" }),
        ["multi-switch"]   = ("per-bit", new[] { "per-bit" },                         new[] { "vector" }),
        ["matrix-3x3"]     = ("value",   new[] { "raw" },                             new[] { "value" }),
        ["matrix-3x3-rgb"] = ("planes",  new[] { "raw" },                             new[] { "planes" }),
        ["buzzer-piezo"]   = ("freq",    new[] { "signal" },                          new[] { "freq" }),
        ["keypad-matrix"]  = ("decoded", new[] { "raw-scan" },                        new[] { "decoded" }),
        ["lcd-hd44780"]    = ("text",    new[] { "raw-4bit", "raw-8bit", "raw-panel" }, new[] { "text" }),
    };

    /// <summary>
    /// The mode stored in the component's config, normalized to a known mode
    /// (missing/unknown → the widget's legacy default). Ignores Advanced mode.
    /// Returns an empty string for widgets without modes.
    /// </summary>
    public static string StoredMode(KvizComponent c)
    {
        if (!ModeSpec.TryGetValue(c.Type, out var spec)) return string.Empty;
        var m = c.Config.GetString("mode", spec.Default);
        return spec.Raw.Contains(m) || spec.Expert.Contains(m) ? m : spec.Default;
    }

    /// <summary>True when <paramref name="mode"/> is an assisted "Expert" mode of <paramref name="widgetType"/>.</summary>
    public static bool IsExpertMode(string widgetType, string mode)
        => ModeSpec.TryGetValue(widgetType, out var spec) && spec.Expert.Contains(mode);

    /// <summary>
    /// The mode the widget actually uses. With Advanced mode ON this is the
    /// stored mode; with it OFF an Expert mode collapses to the widget's raw
    /// pin-level mode (the stored config is left untouched).
    /// </summary>
    public static string EffectiveMode(KvizComponent c, bool advanced)
    {
        var m = StoredMode(c);
        if (advanced || m.Length == 0) return m;
        var spec = ModeSpec[c.Type];
        return spec.Expert.Contains(m) ? spec.Raw[0] : m;
    }

    /// <summary>Returns the pin surface for the given component based on its type and config.</summary>
    /// <param name="c">The component to inspect.</param>
    /// <returns>The component's pins.</returns>
    public static IEnumerable<KvizPin> PinsOf(KvizComponent c) => PinsOf(c, advanced: true);

    /// <summary>
    /// Advanced-aware pin surface: with <paramref name="advanced"/> OFF the
    /// pins are those of the widget's <see cref="EffectiveMode"/> (raw).
    /// </summary>
    public static IEnumerable<KvizPin> PinsOf(KvizComponent c, bool advanced)
    {
        if (advanced) return PinsForMode(c, null, null);
        return PinsForMode(c, EffectiveMode(c, advanced: false), EffectiveWiring(c, advanced: false));
    }

    /// <summary>
    /// 7-seg bank wiring actually used. <c>packed</c> (one vector per bus:
    /// raw <c>value</c>, mux <c>seg</c>+<c>sel</c>) is an Advanced-only
    /// convenience; with Advanced OFF the bank is always wired pin-by-pin
    /// (<c>explicit</c>), like the real glass. Other widgets: empty string.
    /// </summary>
    public static string EffectiveWiring(KvizComponent c, bool advanced)
    {
        if (c.Type != "seven-seg-bank") return string.Empty;
        if (!advanced) return "explicit";
        return c.Config.GetString("wiring", "packed") == "explicit" ? "explicit" : "packed";
    }

    // overrides == null → read config exactly as before (legacy defaults).
    private static IEnumerable<KvizPin> PinsForMode(KvizComponent c, string? modeOverride, string? wiringOverride)
    {
        string M(string def) => modeOverride ?? c.Config.GetString("mode", def);
        return c.Type switch
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
        "seven-seg"    => SevenSegPins(M("raw")),
        "seven-seg-bank" => SevenSegBankPins(
                                M("raw"),
                                wiringOverride ?? c.Config.GetString("wiring", "packed"),
                                c.Config.GetInt("digits", 4)),
        "multi-switch" => MultiSwitchPins(
                              M("per-bit"),
                              c.Config.GetInt("count", 4)),
        "matrix-3x3"   => LedMatrix33Pins(M("value")),
        "matrix-3x3-rgb" => RgbMatrix33Pins(M("planes")),
        "buzzer-piezo" => BuzzerPins(M("freq")),
        "keypad-matrix" => KeypadPins(
            M("decoded"),
            c.Config.GetInt("rows", 4),
            c.Config.GetInt("cols", 4)),
        "lcd-hd44780" => LcdPins(
            M("text"),
            c.Config.GetInt("rows", 2),
            c.Config.GetInt("cols", 16),
            c.Config.GetInt("glassRows", 8),
            c.Config.GetInt("glassCols", 5)),
        "dot-matrix" => DotMatrixPins(
            c.Config.GetInt("rows", 8),
            c.Config.GetInt("cols", 8)),
        _ => Array.Empty<KvizPin>(),
        };
    }

    private static KvizPin[] SevenSegPins(string mode) => mode == "raw"
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
    private static KvizPin[] MultiSwitchPins(string mode, int count)
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
    private static KvizPin[] LedMatrix33Pins(string mode)
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
    private static KvizPin[] RgbMatrix33Pins(string mode)
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
    private static KvizPin[] BuzzerPins(string mode)
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
    //   • "raw-panel" — the LCD glass with the HD44780 removed: a passive
    //     multiplexed dot-matrix. The design drives the raw lines itself —
    //     `COM0..COM{glassRows-1}` (common/backplane row selects) + `SEG0..
    //     SEG{glassCols-1}` (segment/column data) — and implements the scan
    //     multiplexing + glyph patterns. Default a single 5×8 character cell.
    private static List<KvizPin> LcdPins(string mode, int rows, int cols, int glassRows = 8, int glassCols = 5)
    {
        if (mode == "raw-panel")
        {
            int gr = Math.Clamp(glassRows, 1, 16);
            int gc = Math.Clamp(glassCols, 1, 40);
            var panel = new List<KvizPin>(gr + gc);
            for (int r = 0; r < gr; r++)
                panel.Add(new KvizPin($"COM{r}", PinDir.In));   // backplane row select
            for (int col = 0; col < gc; col++)
                panel.Add(new KvizPin($"SEG{col}", PinDir.In)); // segment column data
            return panel;
        }
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

    // Dot-matrix LED display pin surface. A controller-less (no MAX7219)
    // scanned matrix: the design drives `col0..col{cols-1}` (column selects,
    // one active at a time) + `row0..row{rows-1}` (row data for the selected
    // column), implementing the scan multiplexing + font ROM itself. All pins
    // are In (the design drives both selects and data); the widget rebuilds
    // the image by persistence of vision from the frame ring.
    private static List<KvizPin> DotMatrixPins(int rows, int cols)
    {
        rows = Math.Clamp(rows, 1, 16);
        cols = Math.Clamp(cols, 1, 16);
        var pins = new List<KvizPin>(rows + cols);
        for (int c = 0; c < cols; c++)
            pins.Add(new KvizPin($"col{c}", PinDir.In)); // column select
        for (int r = 0; r < rows; r++)
            pins.Add(new KvizPin($"row{r}", PinDir.In)); // row data
        return pins;
    }

    // 7-segment bank pin surface. Three "independent" decode modes keep the
    // original single packed `value` bus (one dedicated slice per digit); the
    // "mux" mode models a real multiplexed display — shared segment lines +
    // one per-digit common/select (anode/cathode) line. In mux, the `wiring`
    // config picks how those lines are exposed (raw mode too — 2026-11):
    //   • "packed"   — two vectors: `seg` (7-bit, MSB=a) + `sel` (digits-bit,
    //                  MSB-first so char 0 = leftmost digit N-1). Compact.
    //   • "explicit" — individual pins: `a..g` + `an0..an{digits-1}`, each
    //                  separately bindable (verbose but precise).
    // Raw (non-mux) mode: "packed" = the single `value` bus; "explicit" =
    // one pin per segment per digit, `d{N}_{a..g}` (digit 0 = rightmost) —
    // the real non-multiplexed glass. With Advanced OFF wiring is always
    // explicit (see EffectiveWiring).
    // All pins are In (the design drives both segments and selects).
    /// <summary>Pin name of segment <paramref name="seg"/> (a..g) of digit <paramref name="digit"/> in a raw explicit 7-seg bank.</summary>
    public static string BankDigitPin(int digit, string seg) => $"d{digit}_{seg}";

    private static IEnumerable<KvizPin> SevenSegBankPins(string mode, string wiring, int digits)
    {
        var n = Math.Clamp(digits, 2, 8);
        if (mode == "raw" && wiring == "explicit")
        {
            var pins = new List<KvizPin>(7 * n);
            for (int d = 0; d < n; d++)
                foreach (var seg in new[] { "a", "b", "c", "d", "e", "f", "g" })
                    pins.Add(new KvizPin(BankDigitPin(d, seg), PinDir.In));
            return pins;
        }
        if (mode != "mux")
            return new[] { new KvizPin("value", PinDir.In) };

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
    /// <summary>Counts a component's input and output pins separately (for mixed-direction widgets like the keypad).</summary>
    /// <param name="c">The component to inspect.</param>
    /// <returns>A tuple of the input pin count and output pin count.</returns>
    /// <param name="advanced">Advanced mode; OFF counts the raw (effective) pin surface.</param>
    public static (int InCount, int OutCount) PinCountsByDir(KvizComponent c, bool advanced = true)
    {
        int inC = 0, outC = 0;
        foreach (var p in PinsOf(c, advanced))
        {
            if (p.Direction == PinDir.In) inC++;
            else outC++;
        }
        return (inC, outC);
    }

    /// <summary>Returns the overall I/O direction of a widget type, or null for mixed-direction widgets.</summary>
    /// <param name="widgetType">The widget type name.</param>
    /// <returns>The widget's direction, or null when it mixes directions (e.g. keypad-matrix).</returns>
    public static WidgetDirection? DirectionOf(string widgetType) => widgetType switch
    {
        "button" or "switch" or "multi-switch"                                                                                                 => WidgetDirection.Input,
        "led" or "led-array" or "seven-seg" or "seven-seg-bank" or "stepper" or "matrix-3x3" or "matrix-3x3-rgb" or "buzzer-piezo" or "lcd-hd44780" or "dot-matrix" => WidgetDirection.Output,
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
    /// <summary>Drops bindings whose pin key is no longer valid for each component's current type and config.</summary>
    /// <param name="doc">The document whose component bindings are sanitized in place.</param>
    /// <returns>The number of stale bindings removed.</returns>
    /// <param name="advanced">
    /// Advanced mode. ON (default) keeps exactly the stored mode's pins. OFF
    /// also keeps the raw (effective) mode's pins, so wiring a widget whose
    /// file carries an Expert mode while Advanced is OFF never deletes either
    /// the author's Expert bindings or the user's raw ones.
    /// </param>
    public static int Sanitize(KvizDoc doc, bool advanced = true)
    {
        if (doc?.Components == null) return 0;
        int dropped = 0;
        foreach (var c in doc.Components)
        {
            var valid = PinsOf(c).Select(p => p.Name).ToHashSet();
            if (!advanced) valid.UnionWith(PinsOf(c, advanced: false).Select(p => p.Name));
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
