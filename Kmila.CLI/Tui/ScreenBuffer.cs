using System.Text;

namespace Kmila.CLI.Tui;

/// <summary>Logical cell attributes (color + emphasis), resolved to ANSI SGR at render time.</summary>
public enum Attr : byte
{
    /// <summary>Terminal default foreground/background.</summary>
    Default = 0,
    /// <summary>Dimmed text (e.g. rulers, placeholders).</summary>
    Dim,
    /// <summary>Cyan — focused panel borders, bus segments.</summary>
    Cyan,
    /// <summary>Green — scalar waveform levels.</summary>
    Green,
    /// <summary>Yellow — the waveform cursor and value readout.</summary>
    Yellow,
    /// <summary>Red — unknown/x/z values and errors.</summary>
    Red,
    /// <summary>Blue.</summary>
    Blue,
    /// <summary>Magenta.</summary>
    Magenta,
    /// <summary>Bright white.</summary>
    White,
    /// <summary>Inverse video — the selection highlight.</summary>
    Selected,
    /// <summary>Bold bright on a blue-ish ground — the title/menu bar.</summary>
    HeaderBar,
    /// <summary>Inverse — the bottom status line.</summary>
    StatusBar,
}

/// <summary>
/// An in-memory character grid the TUI draws into, rendered to the terminal as
/// a single escape-laden string per frame (so the alternate screen updates
/// without flicker). Colors collapse to nothing when the terminal reports no
/// color support, but emphasis (inverse/bold) still applies.
/// </summary>
public sealed class ScreenBuffer
{
    private char[] _chars;
    private Attr[] _attrs;

    /// <summary>Width in columns.</summary>
    public int Width { get; private set; }

    /// <summary>Height in rows.</summary>
    public int Height { get; private set; }

    /// <summary>Creates a buffer of the given size.</summary>
    public ScreenBuffer(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        _chars = new char[Width * Height];
        _attrs = new Attr[Width * Height];
        Clear();
    }

    /// <summary>Resizes the buffer, discarding contents.</summary>
    public void Resize(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        _chars = new char[Width * Height];
        _attrs = new Attr[Width * Height];
        Clear();
    }

    /// <summary>Fills the whole buffer with spaces in the default attribute.</summary>
    public void Clear()
    {
        Array.Fill(_chars, ' ');
        Array.Fill(_attrs, Attr.Default);
    }

    /// <summary>Sets a single cell (bounds-checked; out-of-range is ignored).</summary>
    public void Set(int x, int y, char ch, Attr attr = Attr.Default)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
        int i = y * Width + x;
        _chars[i] = ch;
        _attrs[i] = attr;
    }

    /// <summary>Reads the character at (x,y), or space when out of range.</summary>
    public char CharAt(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return ' ';
        return _chars[y * Width + x];
    }

    /// <summary>Writes a string starting at (x,y), clipped to the row; returns the next free x.</summary>
    public int Text(int x, int y, string? s, Attr attr = Attr.Default)
    {
        if (string.IsNullOrEmpty(s)) return x;
        foreach (var ch in s)
        {
            if (x >= Width) break;
            Set(x++, y, ch, attr);
        }
        return x;
    }

    /// <summary>Writes a string clipped to a maximum column width.</summary>
    public void TextClipped(int x, int y, string s, int maxWidth, Attr attr = Attr.Default)
    {
        if (maxWidth <= 0) return;
        if (s.Length > maxWidth) s = maxWidth <= 1 ? s[..maxWidth] : s[..(maxWidth - 1)] + "…";
        Text(x, y, s, attr);
    }

    /// <summary>Fills a row segment with a character/attr (e.g. a status bar).</summary>
    public void FillRow(int y, int x0, int x1, char ch, Attr attr)
    {
        for (int x = x0; x < x1; x++) Set(x, y, ch, attr);
    }

    /// <summary>Draws a box border; <paramref name="unicode"/> picks box-drawing vs ASCII glyphs.</summary>
    public void Box(int x, int y, int w, int h, bool unicode, Attr attr = Attr.Default, string? title = null)
    {
        if (w < 2 || h < 2) return;
        char tl, tr, bl, br, hz, vt;
        if (unicode) { tl = '┌'; tr = '┐'; bl = '└'; br = '┘'; hz = '─'; vt = '│'; }
        else         { tl = '+'; tr = '+'; bl = '+'; br = '+'; hz = '-'; vt = '|'; }

        Set(x, y, tl, attr);
        Set(x + w - 1, y, tr, attr);
        Set(x, y + h - 1, bl, attr);
        Set(x + w - 1, y + h - 1, br, attr);
        for (int i = 1; i < w - 1; i++) { Set(x + i, y, hz, attr); Set(x + i, y + h - 1, hz, attr); }
        for (int j = 1; j < h - 1; j++) { Set(x, y + j, vt, attr); Set(x + w - 1, y + j, vt, attr); }

        if (!string.IsNullOrEmpty(title) && w > 4)
        {
            var t = " " + title + " ";
            if (t.Length > w - 2) t = t[..(w - 2)];
            Text(x + 1, y, t, attr);
        }
    }

    /// <summary>Renders the whole buffer to one ANSI string for a single write.</summary>
    public string Render(bool useColor)
    {
        var sb = new StringBuilder(Width * Height + Height * 8);
        var current = (Attr)255; // force an SGR at the start of each row
        for (int y = 0; y < Height; y++)
        {
            sb.Append("\x1b[").Append(y + 1).Append(";1H"); // move to row start (1-based)
            current = (Attr)255;
            for (int x = 0; x < Width; x++)
            {
                int i = y * Width + x;
                var a = _attrs[i];
                if (a != current)
                {
                    sb.Append(Sgr(a, useColor));
                    current = a;
                }
                sb.Append(_chars[i]);
            }
            sb.Append("\x1b[0m");
        }
        return sb.ToString();
    }

    private static string Sgr(Attr a, bool color)
    {
        // Emphasis (inverse/bold) always applies; plain colors only when supported.
        return a switch
        {
            Attr.Default   => "\x1b[0m",
            Attr.Selected  => "\x1b[0;7m",
            Attr.StatusBar => "\x1b[0;7m",
            Attr.HeaderBar => color ? "\x1b[1;30;46m" : "\x1b[0;1;7m",
            Attr.Dim       => "\x1b[0;2m",
            Attr.Cyan      => color ? "\x1b[0;36m" : "\x1b[0m",
            Attr.Green     => color ? "\x1b[0;32m" : "\x1b[0m",
            Attr.Yellow    => color ? "\x1b[0;33m" : "\x1b[0m",
            Attr.Red       => color ? "\x1b[0;31m" : "\x1b[0m",
            Attr.Blue      => color ? "\x1b[0;34m" : "\x1b[0m",
            Attr.Magenta   => color ? "\x1b[0;35m" : "\x1b[0m",
            Attr.White     => color ? "\x1b[0;97m" : "\x1b[0m",
            _              => "\x1b[0m",
        };
    }
}
