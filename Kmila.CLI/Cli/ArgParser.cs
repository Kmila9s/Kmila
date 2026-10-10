namespace Kmila.CLI.Cli;

/// <summary>
/// Tiny hand-rolled argument parser in the house style of the repo's other
/// console tools (SimulatorBench etc.) — no external dependency. Supports
/// <c>--flag value</c>, <c>--flag=value</c>, repeatable flags (collected into a
/// list), boolean switches (declared up front so they never swallow the next
/// token), short aliases, and bare positionals. Flag lookups are
/// case-insensitive on the flag name.
/// </summary>
public sealed class ArgParser
{
    private readonly HashSet<string> _switches;
    private readonly Dictionary<string, List<string>> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _positionals = new();

    /// <summary>Bare (non-flag) arguments, in order.</summary>
    public IReadOnlyList<string> Positionals => _positionals;

    /// <summary>
    /// Parses <paramref name="args"/>. <paramref name="switches"/> lists the
    /// names (without leading dashes) of boolean flags that take no value.
    /// </summary>
    public ArgParser(string[] args, params string[] switches)
    {
        _switches = new HashSet<string>(switches, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            var tok = args[i];
            if (tok.Length >= 2 && tok[0] == '-')
            {
                var name = tok.TrimStart('-');
                var eq = name.IndexOf('=');
                if (eq >= 0)
                {
                    Add(name[..eq], name[(eq + 1)..]);
                    continue;
                }
                if (_switches.Contains(name))
                {
                    Add(name, "true");
                    continue;
                }
                // Value flag: consume the next token unless it's another flag.
                if (i + 1 < args.Length && !(args[i + 1].Length >= 2 && args[i + 1][0] == '-'))
                {
                    Add(name, args[++i]);
                }
                else
                {
                    // Lone value-flag with nothing after it — treat as a switch
                    // so `--verbose`-style usage still registers as present.
                    Add(name, "true");
                }
            }
            else
            {
                _positionals.Add(tok);
            }
        }
    }

    private void Add(string name, string value)
    {
        if (!_values.TryGetValue(name, out var list))
            _values[name] = list = new List<string>();
        list.Add(value);
    }

    /// <summary>True when any of the given flag names (aliases) was present.</summary>
    public bool Has(params string[] names) => names.Any(_values.ContainsKey);

    /// <summary>First value for any of the given flag names, or null.</summary>
    public string? Get(params string[] names)
    {
        foreach (var n in names)
            if (_values.TryGetValue(n, out var list) && list.Count > 0)
                return list[0];
        return null;
    }

    /// <summary>All values across the given flag names (for repeatable flags).</summary>
    public IReadOnlyList<string> GetAll(params string[] names)
    {
        var acc = new List<string>();
        foreach (var n in names)
            if (_values.TryGetValue(n, out var list))
                acc.AddRange(list);
        return acc;
    }

    /// <summary>Integer value for any of the given flag names, or <paramref name="fallback"/>.</summary>
    public int GetInt(int fallback, params string[] names)
    {
        var raw = Get(names);
        return int.TryParse(raw, out var v) ? v : fallback;
    }
}
