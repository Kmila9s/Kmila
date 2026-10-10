using Kmila.CLI.I18n;
using Kmila.CLI.Simulation;

namespace Kmila.CLI.Tui;

/// <summary>
/// The full-screen terminal UI: a file browser, a source editor/viewer, and an
/// ASCII waveform viewer, driven by a single key loop over
/// <see cref="AnsiTerminal"/> + <see cref="ScreenBuffer"/>. No third-party TUI
/// toolkit — everything is hand-rolled ANSI so it runs anywhere .NET 9 does.
/// </summary>
public sealed class TuiApp
{
    /// <summary>Which panel currently receives key input.</summary>
    private enum Focus { Files, Editor, Wave }

    private readonly AnsiTerminal _term = new();
    private readonly Strings _s;
    private readonly bool _forceAscii;
    private ScreenBuffer _buf = new(80, 24);

    private string _dir;
    private List<string> _files = new();        // *.vhd/*.vhdl in _dir (names)
    private int _fileSel, _fileScroll;

    private string? _topFile;                    // chosen top file (absolute)
    private string? _openPath;                   // file shown in the editor
    private List<string> _lines = new() { "" };  // editor buffer
    private int _curLine, _curCol, _edScroll;
    private bool _dirty;

    private WaveModel _wave = new();
    private long _viewStartNs;
    private double _nsPerCol = 1;
    private long _cursorNs;
    private long? _markNs;                        // second cursor (measurement marker)
    private int _waveSel, _waveScroll;
    private bool _autoFit = true;
    private int _lastWaveTrackW = 40;
    private readonly HashSet<string> _hidden = new(StringComparer.OrdinalIgnoreCase);
    private string _waveFilter = "";             // name filter for the signal list
    private SimRunResult? _lastResult;           // last sim result (for SVG export)
    private string _lastVcd = "";                // last VCD text (for VCD export)

    private readonly List<string> _log = new();
    private Focus _focus = Focus.Files;
    private string _status = "";
    private bool _running = true;
    private bool _showHelp;

    // Simulation parameters (editable via 'p').
    private long _durationNs = 200;
    private int _clockMhz = 50;
    private int _tickResNs = 10;
    private string? _fpga;

    private int _lastW, _lastH;

    /// <summary>Creates the app.</summary>
    /// <param name="dir">Initial directory for the file browser.</param>
    /// <param name="topFile">Optional file to pre-select as top and open.</param>
    /// <param name="openVcd">Optional existing VCD to load into the viewer.</param>
    /// <param name="lang">UI language.</param>
    /// <param name="forceAscii">ASCII-only glyph mode.</param>
    public TuiApp(string dir, string? topFile, string? openVcd, string? lang, bool forceAscii)
    {
        _s = Strings.Load(lang);
        _forceAscii = forceAscii;
        _dir = Directory.Exists(dir) ? Path.GetFullPath(dir) : Directory.GetCurrentDirectory();
        RefreshFiles();

        if (!string.IsNullOrWhiteSpace(topFile) && File.Exists(topFile))
        {
            _topFile = Path.GetFullPath(topFile);
            OpenInEditor(_topFile);
            var idx = _files.FindIndex(f => string.Equals(
                Path.GetFullPath(Path.Combine(_dir, f)), _topFile, StringComparison.Ordinal));
            if (idx >= 0) _fileSel = idx;
        }

        if (!string.IsNullOrWhiteSpace(openVcd) && File.Exists(openVcd))
        {
            try { _lastVcd = File.ReadAllText(openVcd); _wave = VcdReader.Parse(_lastVcd); _autoFit = true; _status = $"VCD: {Path.GetFileName(openVcd)}"; }
            catch (Exception ex) { _status = "VCD error: " + ex.Message; }
        }
    }

    /// <summary>Runs the UI loop until the user quits. Returns a process exit code.</summary>
    public int Run()
    {
        try
        {
            _term.Enter(_forceAscii);
            bool needRender = true;
            while (_running)
            {
                if (_term.Width != _lastW || _term.Height != _lastH)
                {
                    _lastW = _term.Width; _lastH = _term.Height;
                    _buf.Resize(_lastW, _lastH);
                    needRender = true;
                }

                if (needRender) { Render(); needRender = false; }

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    HandleKey(key);
                    needRender = true;
                }
                else
                {
                    Thread.Sleep(20);
                }
            }
        }
        finally
        {
            _term.Exit();
        }
        return 0;
    }

    // ---- File browser ------------------------------------------------------

    /// <summary>Rescans the current directory for <c>*.vhd</c>/<c>*.vhdl</c> files.</summary>
    private void RefreshFiles()
    {
        try
        {
            _files = Directory.EnumerateFiles(_dir)
                .Where(f => f.EndsWith(".vhd", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".vhdl", StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileName)
                .Where(n => n != null).Select(n => n!)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch { _files = new(); }
        if (_fileSel >= _files.Count) _fileSel = Math.Max(0, _files.Count - 1);
    }

    /// <summary>Loads a file into the editor buffer and resets the cursor/dirty state.</summary>
    private void OpenInEditor(string path)
    {
        try
        {
            _lines = File.ReadAllLines(path).ToList();
            if (_lines.Count == 0) _lines.Add("");
            _openPath = path;
            _curLine = _curCol = _edScroll = 0;
            _dirty = false;
        }
        catch (Exception ex) { _status = "open error: " + ex.Message; }
    }

    /// <summary>Writes the editor buffer back to its file and clears the dirty flag.</summary>
    private void SaveEditor()
    {
        if (_openPath == null) { _status = "nothing to save"; return; }
        try
        {
            File.WriteAllLines(_openPath, _lines);
            _dirty = false;
            _status = "saved " + Path.GetFileName(_openPath);
        }
        catch (Exception ex) { _status = "save error: " + ex.Message; }
    }

    // ---- Simulation --------------------------------------------------------

    /// <summary>
    /// Runs the current file through the engine and loads the result into the
    /// waveform viewer. If the editor buffer is dirty, a temp copy is simulated
    /// so what is shown is what runs.
    /// </summary>
    private void RunSimulation()
    {
        var src = _openPath ?? _topFile;
        if (src == null) { _status = "select a .vhdl file first (Enter in Files)"; return; }

        // If the open buffer is dirty, simulate the on-disk file after saving a
        // temp copy so what you see is what runs.
        string runFile = src;
        string? temp = null;
        if (_dirty && _openPath != null)
        {
            temp = Path.Combine(Path.GetTempPath(), $"kmila_ed_{Path.GetFileName(_openPath)}");
            try { File.WriteAllLines(temp, _lines); runFile = temp; } catch { temp = null; }
        }

        _status = "simulating…";
        Render();

        var svc = new KmilaSimulationService();
        try
        {
            var req = new SimRunRequest
            {
                TopFilePath = runFile,
                TotalNs = _durationNs,
                TickResNs = _tickResNs,
                SystemClockMhz = _clockMhz,
                Fpga = _fpga,
            };
            var result = svc.RunAsync(req).GetAwaiter().GetResult();
            var vcd = result.History.ExportToVcd(result.Entity, timescaleNs: result.TickResNs);
            _wave = VcdReader.Parse(vcd);
            _lastResult = result; _lastVcd = vcd;
            _log.Clear();
            _log.AddRange(result.Log);
            _autoFit = true; _cursorNs = 0; _markNs = null; _waveSel = 0; _waveScroll = 0; _viewStartNs = 0;
            _focus = Focus.Wave;
            _status = $"OK: {result.Entity}  {result.SignalCount} signals  {result.TotalTicks} ticks  {result.DurationMs}ms";
        }
        catch (Exception ex)
        {
            _status = "sim error: " + ex.Message;
        }
        finally
        {
            if (temp != null) { try { File.Delete(temp); } catch { } }
        }
    }

    /// <summary>The signals currently shown in the waveform panel after filter + hide.</summary>
    private List<WaveSignal> VisibleSignals()
    {
        IEnumerable<WaveSignal> q = _wave.Signals.Where(s => !_hidden.Contains(s.Name));
        if (!string.IsNullOrEmpty(_waveFilter))
            q = q.Where(s => s.Name.Contains(_waveFilter, StringComparison.OrdinalIgnoreCase));
        return q.ToList();
    }

    /// <summary>Exports the current waveform to a timestamped file (VCD, or SVG when a run is loaded).</summary>
    private void ExportWave(bool svg)
    {
        try
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            if (svg)
            {
                if (_lastResult == null) { _status = "SVG export needs a run (F5) first"; return; }
                var path = Path.Combine(_dir, $"kmila-wave-{stamp}.svg");
                File.WriteAllText(path, Simulation.OutputWriter.Render(_lastResult, Simulation.OutputFormat.Svg));
                _status = "exported " + Path.GetFileName(path);
            }
            else
            {
                if (string.IsNullOrEmpty(_lastVcd)) { _status = "nothing to export (run or open a VCD)"; return; }
                var path = Path.Combine(_dir, $"kmila-wave-{stamp}.vcd");
                File.WriteAllText(path, _lastVcd);
                _status = "exported " + Path.GetFileName(path);
            }
        }
        catch (Exception ex) { _status = "export error: " + ex.Message; }
    }

    // ---- Key handling ------------------------------------------------------

    /// <summary>
    /// Top-level key dispatch: global shortcuts (quit/simulate/save/panel-switch)
    /// first, then per-focus handling. In the editor, only modifier/F-key
    /// shortcuts are global so typed characters reach the buffer.
    /// </summary>
    private void HandleKey(ConsoleKeyInfo k)
    {
        if (_showHelp) { _showHelp = false; return; }

        // Global quit that works from any panel, including the editor.
        if (k.Modifiers.HasFlag(ConsoleModifiers.Control) && k.Key == ConsoleKey.Q)
        { _running = false; return; }

        // Global shortcuts that are safe even while editing use modifiers/F-keys.
        if (k.Key == ConsoleKey.F5 ||
            (k.Modifiers.HasFlag(ConsoleModifiers.Control) && k.Key == ConsoleKey.R))
        { RunSimulation(); return; }

        if (k.Modifiers.HasFlag(ConsoleModifiers.Control) && k.Key == ConsoleKey.S)
        { SaveEditor(); return; }

        if (k.Key == ConsoleKey.Tab)
        { _focus = (Focus)(((int)_focus + 1) % 3); return; }

        if (_focus == Focus.Editor)
        {
            HandleEditorKey(k);
            return;
        }

        // Navigation-mode shortcuts (Files / Wave).
        switch (k.Key)
        {
            case ConsoleKey.Q or ConsoleKey.Escape:
                _running = false; return;
            case ConsoleKey.F1:
                _showHelp = true; return;
        }
        switch (char.ToLowerInvariant(k.KeyChar))
        {
            case 'q': _running = false; return;
            case '?': _showHelp = true; return;
            case 'r': RunSimulation(); return;
            case 'p': PromptParams(); return;
            case 'o': _focus = Focus.Files; return;
            case 'e': _focus = Focus.Editor; return;
            case 'w': _focus = Focus.Wave; return;
        }

        if (_focus == Focus.Files) HandleFilesKey(k);
        else HandleWaveKey(k);
    }

    /// <summary>Keys for the file browser: navigate, and Enter to set top &amp; open.</summary>
    private void HandleFilesKey(ConsoleKeyInfo k)
    {
        switch (k.Key)
        {
            case ConsoleKey.UpArrow:   if (_fileSel > 0) _fileSel--; break;
            case ConsoleKey.DownArrow: if (_fileSel < _files.Count - 1) _fileSel++; break;
            case ConsoleKey.Home:      _fileSel = 0; break;
            case ConsoleKey.End:       _fileSel = Math.Max(0, _files.Count - 1); break;
            case ConsoleKey.Enter:
                if (_fileSel >= 0 && _fileSel < _files.Count)
                {
                    var path = Path.GetFullPath(Path.Combine(_dir, _files[_fileSel]));
                    _topFile = path;
                    OpenInEditor(path);
                    _status = "top: " + _files[_fileSel];
                }
                break;
            case ConsoleKey.F2:
                RefreshFiles(); break;
        }
        switch (char.ToLowerInvariant(k.KeyChar))
        {
            case 'j': if (_fileSel < _files.Count - 1) _fileSel++; break;
            case 'k': if (_fileSel > 0) _fileSel--; break;
        }
    }

    /// <summary>Keys for the waveform viewer: select, move cursor, zoom, scroll, measure, filter, hide, export.</summary>
    private void HandleWaveKey(ConsoleKeyInfo k)
    {
        long span = (long)(_nsPerCol * _lastWaveTrackW);
        int visCount = VisibleSignals().Count;
        switch (k.Key)
        {
            case ConsoleKey.UpArrow:   if (_waveSel > 0) _waveSel--; break;
            case ConsoleKey.DownArrow: if (_waveSel < visCount - 1) _waveSel++; break;
            case ConsoleKey.LeftArrow: _cursorNs = Math.Max(0, _cursorNs - (long)Math.Ceiling(_nsPerCol)); EnsureCursorVisible(); break;
            case ConsoleKey.RightArrow: _cursorNs += (long)Math.Ceiling(_nsPerCol); EnsureCursorVisible(); break;
            case ConsoleKey.Home: _cursorNs = 0; _viewStartNs = 0; break;
            case ConsoleKey.End: _cursorNs = _wave.MaxTimeFs / WaveModel.FsPerNs; EnsureCursorVisible(); break;
            case ConsoleKey.PageUp: _viewStartNs = Math.Max(0, _viewStartNs - span); break;
            case ConsoleKey.PageDown: _viewStartNs += span; break;
        }
        switch (k.KeyChar)
        {
            case '+' or '=': _nsPerCol = Math.Max(0.1, _nsPerCol / 2); _autoFit = false; break;
            case '-' or '_': _nsPerCol *= 2; _autoFit = false; break;
            case '<' or ',': _viewStartNs = Math.Max(0, _viewStartNs - Math.Max(1, span / 4)); break;
            case '>' or '.': _viewStartNs += Math.Max(1, span / 4); break;
            case 'j': if (_waveSel < visCount - 1) _waveSel++; break;
            case 'k': if (_waveSel > 0) _waveSel--; break;
            case '0': _viewStartNs = 0; _autoFit = true; break;
            case 'm':   // set/clear the measurement marker at the cursor
                _markNs = _markNs == _cursorNs ? null : _cursorNs;
                _status = _markNs is { } mk ? $"marca @ {mk}ns (Δ en la barra)" : "marca borrada";
                break;
            case 'h':   // hide the selected signal
            {
                var visH = VisibleSignals();
                if (_waveSel >= 0 && _waveSel < visH.Count)
                {
                    _hidden.Add(visH[_waveSel].Name);
                    if (_waveSel >= VisibleSignals().Count) _waveSel = Math.Max(0, VisibleSignals().Count - 1);
                    _status = "señal oculta (a = mostrar todo)";
                }
                break;
            }
            case 'a':   // show all (clear hidden + filter)
                _hidden.Clear(); _waveFilter = ""; _status = "todas las señales visibles";
                break;
            case '/':   // filter by name
                _waveFilter = Prompt($"Filtro señales [{_waveFilter}]: ").Trim();
                _waveSel = 0; _waveScroll = 0;
                break;
            case 'S':   // export waveform
            {
                var fmt = Prompt("Exportar (v=vcd, s=svg): ").Trim().ToLowerInvariant();
                if (fmt.StartsWith('s')) ExportWave(svg: true);
                else if (fmt.StartsWith('v')) ExportWave(svg: false);
                break;
            }
        }
    }

    /// <summary>Scrolls the time window so the cursor stays on screen.</summary>
    private void EnsureCursorVisible()
    {
        long span = (long)(_nsPerCol * _lastWaveTrackW);
        if (span <= 0) return;
        if (_cursorNs < _viewStartNs) _viewStartNs = _cursorNs;
        else if (_cursorNs >= _viewStartNs + span) _viewStartNs = Math.Max(0, _cursorNs - span + 1);
    }

    /// <summary>Text-editing keys: cursor movement, Enter/Backspace/Delete/Tab, and insertion.</summary>
    private void HandleEditorKey(ConsoleKeyInfo k)
    {
        switch (k.Key)
        {
            case ConsoleKey.Escape: _focus = Focus.Files; return;
            case ConsoleKey.UpArrow: if (_curLine > 0) { _curLine--; ClampCol(); } return;
            case ConsoleKey.DownArrow: if (_curLine < _lines.Count - 1) { _curLine++; ClampCol(); } return;
            case ConsoleKey.LeftArrow: if (_curCol > 0) _curCol--; else if (_curLine > 0) { _curLine--; _curCol = _lines[_curLine].Length; } return;
            case ConsoleKey.RightArrow: if (_curCol < _lines[_curLine].Length) _curCol++; else if (_curLine < _lines.Count - 1) { _curLine++; _curCol = 0; } return;
            case ConsoleKey.Home: _curCol = 0; return;
            case ConsoleKey.End: _curCol = _lines[_curLine].Length; return;
            case ConsoleKey.PageUp: _curLine = Math.Max(0, _curLine - 10); ClampCol(); return;
            case ConsoleKey.PageDown: _curLine = Math.Min(_lines.Count - 1, _curLine + 10); ClampCol(); return;
            case ConsoleKey.Enter:
            {
                var line = _lines[_curLine];
                var tail = line[_curCol..];
                _lines[_curLine] = line[.._curCol];
                _lines.Insert(_curLine + 1, tail);
                _curLine++; _curCol = 0; _dirty = true; return;
            }
            case ConsoleKey.Backspace:
            {
                if (_curCol > 0)
                {
                    var line = _lines[_curLine];
                    _lines[_curLine] = line[..(_curCol - 1)] + line[_curCol..];
                    _curCol--; _dirty = true;
                }
                else if (_curLine > 0)
                {
                    _curCol = _lines[_curLine - 1].Length;
                    _lines[_curLine - 1] += _lines[_curLine];
                    _lines.RemoveAt(_curLine);
                    _curLine--; _dirty = true;
                }
                return;
            }
            case ConsoleKey.Delete:
            {
                var line = _lines[_curLine];
                if (_curCol < line.Length) { _lines[_curLine] = line[.._curCol] + line[(_curCol + 1)..]; _dirty = true; }
                else if (_curLine < _lines.Count - 1) { _lines[_curLine] += _lines[_curLine + 1]; _lines.RemoveAt(_curLine + 1); _dirty = true; }
                return;
            }
            case ConsoleKey.Tab:
                _lines[_curLine] = _lines[_curLine][.._curCol] + "    " + _lines[_curLine][_curCol..];
                _curCol += 4; _dirty = true; return;
        }

        if (!char.IsControl(k.KeyChar))
        {
            var line = _lines[_curLine];
            _lines[_curLine] = line[.._curCol] + k.KeyChar + line[_curCol..];
            _curCol++; _dirty = true;
        }
    }

    /// <summary>Keeps the editor column within the current line's length.</summary>
    private void ClampCol() => _curCol = Math.Min(_curCol, _lines[_curLine].Length);

    // ---- Modal parameter prompt -------------------------------------------

    /// <summary>Prompts (at the status row) for duration / clock / FPGA and updates the run params.</summary>
    private void PromptParams()
    {
        var dur = Prompt($"Duración (ej. 200ns) [{_durationNs}ns]: ");
        if (!string.IsNullOrWhiteSpace(dur))
        {
            try { _durationNs = DurationParser.ParseToNs(dur); } catch { _status = "duración inválida"; }
        }
        var clk = Prompt($"Reloj MHz [{_clockMhz}]: ");
        if (int.TryParse(clk, out var c) && c > 0) _clockMhz = c;
        var fp = Prompt($"FPGA (vacío=ninguno) [{_fpga ?? "-"}]: ");
        if (!string.IsNullOrWhiteSpace(fp))
        {
            if (FpgaResolver.Load(fp) != null) { _fpga = fp; _clockMhz = FpgaResolver.DefaultClockMhz(fp) ?? _clockMhz; }
            else _status = $"FPGA desconocida: {fp}";
        }
        _status = $"params: {_durationNs}ns {_clockMhz}MHz {(_fpga ?? "")}".Trim();
    }

    /// <summary>Reads a single line of input at the status row (modal).</summary>
    private string Prompt(string label)
    {
        int row = _buf.Height;                 // status row (1-based bottom)
        var input = new System.Text.StringBuilder();
        _term.Write("\x1b[?25h");              // show cursor during input
        while (true)
        {
            _term.MoveTo(row, 1);
            _term.Write("\x1b[0;7m");
            var text = (label + input).PadRight(_buf.Width);
            if (text.Length > _buf.Width) text = text[.._buf.Width];
            _term.Write(text);
            _term.Write("\x1b[0m");
            _term.Flush();

            var k = Console.ReadKey(intercept: true);
            if (k.Key == ConsoleKey.Enter) break;
            if (k.Key == ConsoleKey.Escape) { input.Clear(); break; }
            if (k.Key == ConsoleKey.Backspace) { if (input.Length > 0) input.Length--; continue; }
            if (!char.IsControl(k.KeyChar)) input.Append(k.KeyChar);
        }
        _term.Write("\x1b[?25l");              // hide cursor again
        return input.ToString();
    }

    // ---- Rendering ---------------------------------------------------------

    /// <summary>Lays out and paints the whole frame: header, three panels, status bar, help overlay.</summary>
    private void Render()
    {
        bool uni = _term.UseUnicode;
        _buf.Clear();
        int W = _buf.Width, H = _buf.Height;

        // Header bar.
        _buf.FillRow(0, 0, W, ' ', Attr.HeaderBar);
        _buf.Text(1, 0, $" Kmila CLI {Program.Version}  ", Attr.HeaderBar);
        _buf.Text(22, 0, "[Tab] panel  [F5/r] simular  [p] params  [?] ayuda  [q] salir", Attr.HeaderBar);

        int bodyTop = 1, bodyH = H - 2;
        if (bodyH < 3) { Flush(uni); return; }

        int leftW = Math.Clamp(W * 28 / 100, 20, 36);
        int rightX = leftW;
        int rightW = W - leftW;

        DrawFiles(0, bodyTop, leftW, bodyH, uni);

        int edH = Math.Max(4, bodyH / 2);
        int waveH = bodyH - edH;
        DrawEditor(rightX, bodyTop, rightW, edH, uni);
        DrawWave(rightX, bodyTop + edH, rightW, waveH, uni);

        // Status bar.
        _buf.FillRow(H - 1, 0, W, ' ', Attr.StatusBar);
        string foc = _focus switch { Focus.Files => "ARCHIVOS", Focus.Editor => "EDITOR", _ => "ONDAS" };
        string left = $" {foc} ";
        string cur = _focus == Focus.Wave && !_wave.IsEmpty ? $"  t={_cursorNs}ns" : "";
        _buf.Text(0, H - 1, left, Attr.StatusBar);
        _buf.TextClipped(left.Length, H - 1, _status + cur, W - left.Length - 1, Attr.StatusBar);

        if (_showHelp) DrawHelp(uni);

        Flush(uni);
    }

    /// <summary>Renders the screen buffer to the terminal in one write.</summary>
    private void Flush(bool uni)
    {
        _term.Write(_buf.Render(_term.UseColor));
        _term.Flush();
    }

    /// <summary>Draws the file-browser panel (list + top-file marker + scroll).</summary>
    private void DrawFiles(int x, int y, int w, int h, bool uni)
    {
        var attr = _focus == Focus.Files ? Attr.Cyan : Attr.Default;
        _buf.Box(x, y, w, h, uni, attr, $"Archivos ({_files.Count})");
        int rows = h - 2;
        if (_fileSel < _fileScroll) _fileScroll = _fileSel;
        if (_fileSel >= _fileScroll + rows) _fileScroll = _fileSel - rows + 1;
        for (int r = 0; r < rows; r++)
        {
            int fi = _fileScroll + r;
            if (fi >= _files.Count) break;
            bool sel = fi == _fileSel;
            string name = _files[fi];
            bool isTop = _topFile != null &&
                string.Equals(Path.GetFullPath(Path.Combine(_dir, name)), _topFile, StringComparison.Ordinal);
            string mark = isTop ? (uni ? "● " : "* ") : "  ";
            _buf.TextClipped(x + 1, y + 1 + r, mark + name, w - 2, sel ? Attr.Selected : Attr.Default);
        }
        if (_files.Count == 0)
            _buf.TextClipped(x + 1, y + 1, "(sin .vhd/.vhdl)", w - 2, Attr.Dim);
    }

    /// <summary>Draws the editor panel (line numbers + text + cursor highlight).</summary>
    private void DrawEditor(int x, int y, int w, int h, bool uni)
    {
        var attr = _focus == Focus.Editor ? Attr.Cyan : Attr.Default;
        string name = _openPath != null ? Path.GetFileName(_openPath) : "(ninguno)";
        string title = $"Editor: {name}{(_dirty ? " *" : "")}";
        _buf.Box(x, y, w, h, uni, attr, title);

        int rows = h - 2, cols = w - 2;
        if (_curLine < _edScroll) _edScroll = _curLine;
        if (_curLine >= _edScroll + rows) _edScroll = _curLine - rows + 1;

        for (int r = 0; r < rows; r++)
        {
            int li = _edScroll + r;
            if (li >= _lines.Count) break;
            string ln = _lines[li];
            string num = (li + 1).ToString().PadLeft(4) + " ";
            _buf.Text(x + 1, y + 1 + r, num, Attr.Dim);
            // Syntax-highlighted line content, clipped to the panel width.
            int textX = x + 1 + num.Length;
            int textW = cols - num.Length;
            var cattr = VhdlHighlighter.Colorize(ln);
            for (int cc = 0; cc < ln.Length && cc < textW; cc++)
                _buf.Set(textX + cc, y + 1 + r, ln[cc], cattr[cc]);
            // Cursor cell highlight when editing.
            if (_focus == Focus.Editor && li == _curLine)
            {
                int cx = x + 1 + num.Length + Math.Min(_curCol, Math.Max(0, cols - num.Length - 1));
                _buf.Set(cx, y + 1 + r, _buf.CharAt(cx, y + 1 + r), Attr.Selected);
            }
        }
    }

    /// <summary>Draws the waveform panel: visible signals, cursor + marker, value and Δt/frequency readouts.</summary>
    private void DrawWave(int x, int y, int w, int h, bool uni)
    {
        var attr = _focus == Focus.Wave ? Attr.Cyan : Attr.Default;
        var vis = VisibleSignals();
        string flt = string.IsNullOrEmpty(_waveFilter) ? "" : $" /{_waveFilter}";
        string title = _wave.IsEmpty ? "Ondas (F5 para simular)"
            : $"Ondas: {_wave.Module} ({vis.Count}/{_wave.Signals.Count}{flt})";
        _buf.Box(x, y, w, h, uni, attr, title);

        int ix = x + 1, iy = y + 1, iw = w - 2, ih = h - 2;
        if (_wave.IsEmpty)
        {
            _buf.TextClipped(ix, iy, "Sin datos. Selecciona un archivo (Enter) y pulsa F5 o r.", iw, Attr.Dim);
            return;
        }
        if (vis.Count == 0)
        {
            _buf.TextClipped(ix, iy, "(sin señales visibles — 'a' para mostrar todo)", iw, Attr.Dim);
            return;
        }

        // The renderer draws a WaveModel; wrap the visible signals in a view.
        var view = new WaveModel { MaxTimeFs = _wave.MaxTimeFs, Module = _wave.Module };
        foreach (var sg in vis) view.Signals.Add(sg);

        int labelW = Math.Clamp(iw / 4, 8, 20);
        _lastWaveTrackW = Math.Max(2, iw - labelW);
        if (_autoFit)
        {
            long totalNs = Math.Max(1, _wave.MaxTimeFs / WaveModel.FsPerNs);
            _nsPerCol = Math.Max(0.1, (double)totalNs / _lastWaveTrackW);
            _viewStartNs = 0;
            _autoFit = false;
        }
        if (_waveSel >= vis.Count) _waveSel = vis.Count - 1;
        if (_waveSel < _waveScroll) _waveScroll = _waveSel;
        if (_waveSel >= _waveScroll + (ih - 1)) _waveScroll = _waveSel - (ih - 1) + 1;

        WaveformRenderer.Draw(_buf, ix, iy, iw, ih, view,
            _viewStartNs, _nsPerCol, _cursorNs, _waveScroll, _waveSel, uni);

        // Second cursor (measurement marker): recolor its column over the tracks.
        if (_markNs is { } mk)
        {
            int trackX = ix + labelW;
            int col = (int)Math.Round((mk - _viewStartNs) / _nsPerCol);
            if (col >= 0 && col < _lastWaveTrackW)
                for (int r = 0; r < ih - 1; r++)
                {
                    int cx = trackX + col, cy = iy + r;
                    _buf.Set(cx, cy, _buf.CharAt(cx, cy) == ' ' ? (uni ? '┊' : ':') : _buf.CharAt(cx, cy), Attr.Magenta);
                }
        }

        // Selected-signal value at cursor, on the box's title-right if room.
        if (_waveSel >= 0 && _waveSel < vis.Count)
        {
            var sig = vis[_waveSel];
            var v = sig.ValueAtFs(_cursorNs * WaveModel.FsPerNs) ?? "-";
            if (sig.IsBus) v = WaveformRenderer.FormatBus(v, sig.Width);
            string tag = $" {sig.Name}={v} ";
            if (_markNs is { } m2)
            {
                long dt = Math.Abs(_cursorNs - m2);
                string f = dt > 0 ? $" Δ={dt}ns f={1000.0 / dt:0.###}MHz " : " Δ=0ns ";
                tag += f;
            }
            if (tag.Length < w - 2) _buf.Text(x + w - 1 - tag.Length, y, tag, Attr.Yellow);
        }
    }

    /// <summary>Draws the centered help overlay listing the key bindings.</summary>
    private void DrawHelp(bool uni)
    {
        int w = Math.Min(62, _buf.Width - 4), h = Math.Min(21, _buf.Height - 4);
        int x = (_buf.Width - w) / 2, y = (_buf.Height - h) / 2;
        for (int r = 0; r < h; r++) _buf.FillRow(y + r, x, x + w, ' ', Attr.Default);
        _buf.Box(x, y, w, h, uni, Attr.Cyan, "Ayuda");
        string[] help =
        {
            "Tab        cambiar de panel (Archivos/Editor/Ondas)",
            "Enter      (Archivos) fijar como top y abrir",
            "F5 / r     simular el archivo actual",
            "Ctrl+S     guardar el editor",
            "p          editar parámetros (duración/reloj/FPGA)",
            "↑ ↓        navegar / seleccionar señal",
            "← →        (Ondas) mover cursor",
            "+ -        (Ondas) zoom   < >  desplazar tiempo",
            "0          (Ondas) ajustar a la ventana",
            "m          (Ondas) marca para medir Δt / frecuencia",
            "h / a      (Ondas) ocultar señal / mostrar todas",
            "/          (Ondas) filtrar señales por nombre",
            "S          (Ondas) exportar (vcd/svg)",
            "e / o / w  ir a Editor / Archivos / Ondas",
            "? / F1     esta ayuda      q / Esc  salir",
            "",
            "Pulsa cualquier tecla para cerrar.",
        };
        for (int i = 0; i < help.Length && i < h - 2; i++)
            _buf.TextClipped(x + 2, y + 1 + i, help[i], w - 4, Attr.Default);
    }
}
