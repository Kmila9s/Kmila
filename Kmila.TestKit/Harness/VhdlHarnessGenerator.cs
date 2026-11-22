using System.Globalization;
using System.Text;
using Interpreter.Models;
using Kmila.Frontend;

namespace Kmila.TestKit.Harness;

/// <summary>
/// VHDL testbench for a <see cref="HarnessSpec"/>: one signal per DUT port
/// (<c>std_logic</c> or <c>std_logic_vector(w-1 downto 0)</c>, with any initial
/// value), the DUT by direct entity instantiation, a process per clock and one
/// process that applies the timed inputs and then waits forever.
/// </summary>
public sealed class VhdlHarnessGenerator : IHarnessGenerator
{
    /// <inheritdoc />
    public HdlLanguage Language => HdlLanguages.Vhdl;

    /// <inheritdoc />
    public string TopName => "kmila_harness";

    /// <inheritdoc />
    public string Generate(HarnessSpec spec)
    {
        var ports = spec.Ports.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder();
        sb.AppendLine("library ieee;");
        sb.AppendLine("use ieee.std_logic_1164.all;");
        sb.AppendLine($"entity {TopName} is");
        sb.AppendLine($"end entity {TopName};");
        sb.AppendLine($"architecture generated of {TopName} is");
        foreach (var p in spec.Ports)
        {
            string init = spec.Initials.TryGetValue(p.Name, out var v) ? $" := {Literal(p, v)}" : "";
            sb.AppendLine($"  signal {p.Name} : {Type(p)}{init};");
        }
        sb.AppendLine("begin");
        sb.AppendLine($"  dut: entity work.{spec.Top} port map (");
        sb.AppendLine(string.Join(",\n", spec.Ports.Select(p => $"    {p.Name} => {p.Name}")));
        sb.AppendLine("  );");
        foreach (var c in spec.Clocks)
        {
            sb.AppendLine("  process");
            sb.AppendLine("  begin");
            sb.AppendLine($"    {c.Port} <= '{(c.InitialHigh ? '1' : '0')}';");
            sb.AppendLine("    loop");
            sb.AppendLine($"      wait for {Ns(c.HalfPeriodNs)};");
            sb.AppendLine($"      {c.Port} <= not {c.Port};");
            sb.AppendLine("    end loop;");
            sb.AppendLine("  end process;");
        }
        sb.AppendLine("  process");
        sb.AppendLine("  begin");
        long now = 0;
        foreach (var group in spec.Events.GroupBy(e => e.TimeNs).OrderBy(g => g.Key))
        {
            if (group.Key > now) sb.AppendLine($"    wait for {Ns(group.Key - now)};");
            now = group.Key;
            foreach (var e in group)
                sb.AppendLine($"    {e.Port} <= {Literal(ports[e.Port], e.Value)};");
        }
        sb.AppendLine("    wait;");
        sb.AppendLine("  end process;");
        sb.AppendLine("end architecture generated;");
        return sb.ToString();
    }

    private static string Type(HarnessPort p) => p.IsVector ? $"std_logic_vector({p.Width - 1} downto 0)" : "std_logic";

    private static string Ns(long ns) => ns.ToString(CultureInfo.InvariantCulture) + " ns";

    /// <summary>A stimulus value as a VHDL literal of the port's type (bits only: the harness never reinterprets a value).</summary>
    private static string Literal(HarnessPort p, string value)
    {
        string bits = value.Trim().Trim('"', '\'');
        if (bits.Length == 0 || !StdLogic.IsVector(bits))
            throw new NotSupportedException($"harness: value '{value}' for {p.Name} is not a std_logic literal");
        if (!p.IsVector)
        {
            if (bits.Length != 1) throw new NotSupportedException($"harness: value '{value}' does not fit scalar {p.Name}");
            return $"'{bits.ToUpperInvariant()}'";
        }
        if (bits.Length != p.Width) throw new NotSupportedException($"harness: value '{value}' has {bits.Length} bits, {p.Name} has {p.Width}");
        return $"\"{bits.ToUpperInvariant()}\"";
    }
}
