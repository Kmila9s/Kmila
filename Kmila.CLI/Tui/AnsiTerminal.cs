using System.Runtime.InteropServices;
using System.Text;

namespace Kmila.CLI.Tui;

/// <summary>
/// Thin cross-platform terminal control layer built on raw ANSI/VT escape
/// sequences — no third-party dependency. On Windows it first switches the
/// console into virtual-terminal mode (needed by the legacy conhost; a no-op
/// for Windows Terminal) and forces UTF-8 output so box-drawing glyphs render.
/// Honors <c>NO_COLOR</c> and <c>TERM=dumb</c>, and exposes an ASCII-only mode
/// for terminals/fonts without Unicode.
/// </summary>
public sealed class AnsiTerminal : IDisposable
{
    private bool _entered;

    /// <summary>True when color escape sequences should be emitted.</summary>
    public bool UseColor { get; private set; } = true;

    /// <summary>True when Unicode box-drawing/waveform glyphs may be used.</summary>
    public bool UseUnicode { get; private set; } = true;

    /// <summary>Current terminal width in columns (with a sane fallback).</summary>
    public int Width => SafeDim(() => Console.WindowWidth, 80);

    /// <summary>Current terminal height in rows (with a sane fallback).</summary>
    public int Height => SafeDim(() => Console.WindowHeight, 24);

    /// <summary>
    /// Prepares the terminal: VT mode + UTF-8, capability detection, alternate
    /// screen buffer, hidden cursor.
    /// </summary>
    /// <param name="forceAscii">When true, disables Unicode glyphs regardless of encoding.</param>
    public void Enter(bool forceAscii)
    {
        EnableWindowsVirtualTerminal();

        try { Console.OutputEncoding = new UTF8Encoding(false); } catch { /* redirected */ }

        var noColor = Environment.GetEnvironmentVariable("NO_COLOR");
        var term = Environment.GetEnvironmentVariable("TERM");
        UseColor = string.IsNullOrEmpty(noColor) && term != "dumb" && !Console.IsOutputRedirected;
        UseUnicode = !forceAscii && !Console.IsOutputRedirected
                     && (Console.OutputEncoding.CodePage == 65001 ||
                         Console.OutputEncoding.WebName.Contains("utf", StringComparison.OrdinalIgnoreCase));

        Write("\x1b[?1049h"); // enter alternate screen
        Write("\x1b[?25l");   // hide cursor
        Clear();
        try { Console.TreatControlCAsInput = false; } catch { }
        _entered = true;
    }

    /// <summary>Restores the terminal to its prior state.</summary>
    public void Exit()
    {
        if (!_entered) return;
        Write("\x1b[0m");     // reset attributes
        Write("\x1b[?25h");   // show cursor
        Write("\x1b[?1049l"); // leave alternate screen
        Flush();
        _entered = false;
    }

    /// <summary>Clears the screen and homes the cursor.</summary>
    public void Clear() => Write("\x1b[2J\x1b[H");

    /// <summary>Moves the cursor to a 1-based (row, col).</summary>
    public void MoveTo(int row, int col) => Write($"\x1b[{row};{col}H");

    /// <summary>Writes a raw string to stdout (no newline).</summary>
    public void Write(string s) => Console.Out.Write(s);

    /// <summary>Flushes buffered output.</summary>
    public void Flush() => Console.Out.Flush();

    /// <summary>Disposes by leaving the alternate screen cleanly.</summary>
    public void Dispose() => Exit();

    private static int SafeDim(Func<int> read, int fallback)
    {
        try { var v = read(); return v > 0 ? v : fallback; } catch { return fallback; }
    }

    // ---- Windows virtual-terminal enablement -------------------------------

    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    private static void EnableWindowsVirtualTerminal()
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            var handle = GetStdHandle(STD_OUTPUT_HANDLE);
            if (handle == IntPtr.Zero || handle == new IntPtr(-1)) return;
            if (GetConsoleMode(handle, out uint mode))
                SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        }
        catch
        {
            // Older Windows without VT support — the app still runs; glyphs
            // may degrade. UseUnicode detection + --ascii cover that case.
        }
    }
}
