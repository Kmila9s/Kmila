namespace Kmila.CLI.Tui;

/// <summary>
/// A tiny VHDL tokenizer that colors a single editor line for the TUI: keywords,
/// comments, strings/char-literals, and numbers. Deliberately simple (per-line,
/// no cross-line state) — enough to make the editor readable without a full
/// language service.
/// </summary>
public static class VhdlHighlighter
{
    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "entity","architecture","is","of","begin","end","port","generic","map","in","out",
        "inout","buffer","signal","variable","constant","process","if","elsif","else","then",
        "case","when","others","for","loop","while","generate","type","subtype","array","record",
        "downto","to","and","or","not","nand","nor","xor","xnor","with","select","component",
        "use","library","all","function","procedure","return","wait","until","report","severity",
        "std_logic","std_logic_vector","integer","boolean","bit","bit_vector","unsigned","signed",
        "natural","positive","range","after","event","rising_edge","falling_edge","work",
    };

    /// <summary>
    /// Returns a per-character attribute array for <paramref name="line"/>.
    /// Index i is the color for character i.
    /// </summary>
    public static Attr[] Colorize(string line)
    {
        var attrs = new Attr[line.Length];
        int i = 0, n = line.Length;
        while (i < n)
        {
            char ch = line[i];

            // Comment to end of line.
            if (ch == '-' && i + 1 < n && line[i + 1] == '-')
            {
                for (; i < n; i++) attrs[i] = Attr.Dim;
                break;
            }
            // String literal.
            if (ch == '"')
            {
                int start = i++;
                while (i < n && line[i] != '"') i++;
                if (i < n) i++; // closing quote
                for (int k = start; k < i; k++) attrs[k] = Attr.Yellow;
                continue;
            }
            // Character literal 'x' (but not an attribute tick like a'event).
            if (ch == '\'' && i + 2 < n && line[i + 2] == '\'')
            {
                attrs[i] = attrs[i + 1] = attrs[i + 2] = Attr.Yellow;
                i += 3;
                continue;
            }
            // Identifier / keyword.
            if (char.IsLetter(ch) || ch == '_')
            {
                int start = i;
                while (i < n && (char.IsLetterOrDigit(line[i]) || line[i] == '_')) i++;
                var word = line[start..i];
                var attr = Keywords.Contains(word) ? Attr.Cyan : Attr.Default;
                for (int k = start; k < i; k++) attrs[k] = attr;
                continue;
            }
            // Number.
            if (char.IsDigit(ch))
            {
                int start = i;
                while (i < n && (char.IsLetterOrDigit(line[i]) || line[i] == '.' || line[i] == '_')) i++;
                for (int k = start; k < i; k++) attrs[k] = Attr.Green;
                continue;
            }
            attrs[i] = Attr.Default;
            i++;
        }
        return attrs;
    }
}
