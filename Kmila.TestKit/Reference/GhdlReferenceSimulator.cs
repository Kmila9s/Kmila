using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Kmila.Frontend;

namespace Kmila.TestKit.Reference;

/// <summary>
/// GHDL 6 as the VHDL reference: <c>-a</c> on the sources in order, then
/// <c>--elab-run</c> on the top with <c>--vcd</c> and <c>--stop-time</c>, all with
/// <c>-fsynopsys -frelaxed</c>. Works in <c>%TEMP%/kmila-bench/&lt;guid&gt;</c>,
/// deleted afterwards.
/// </summary>
public sealed partial class GhdlReferenceSimulator : IReferenceSimulator
{
    private readonly string _ghdl;

    /// <summary>Uses <paramref name="ghdlPath"/> (default: <c>ghdl</c> on the PATH).</summary>
    public GhdlReferenceSimulator(string ghdlPath = "ghdl") { _ghdl = ghdlPath; }

    /// <inheritdoc />
    public HdlLanguage Language => HdlLanguages.Vhdl;

    /// <inheritdoc />
    public async Task<(bool Installed, string Version)> VersionAsync(CancellationToken ct = default)
    {
        try
        {
            var (rc, output) = await ExecAsync(new[] { "--version" }, ".", ct);
            return (rc == 0, output.Split('\n').FirstOrDefault()?.Trim() ?? "");
        }
        catch (System.ComponentModel.Win32Exception) { return (false, ""); }
    }

    /// <inheritdoc />
    public async Task<ReferenceRun> RunAsync(ReferenceRequest request, CancellationToken ct = default)
    {
        string work = Path.Combine(Path.GetTempPath(), "kmila-bench", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var log = new StringBuilder();
            string[] common = [$"--std={request.Standard ?? "08"}", "-fsynopsys", "-frelaxed"];
            var copies = new List<string>(request.Sources.Count);
            foreach (var src in request.Sources)
            {
                string copy = Path.Combine(work, Path.GetFileName(src));
                File.WriteAllText(copy, SpaceTimeUnits(File.ReadAllText(src)));
                copies.Add(Path.GetFileName(copy));
            }
            // One analysis in the given order (dependencies first), then elaborate and run.
            int rc = await StepAsync(log, work, ct, ["-a", .. common, .. copies]);
            if (rc != 0) return new ReferenceRun(false, null, log.ToString(), rc);
            const string Vcd = "golden.vcd";
            rc = await StepAsync(log, work, ct, ["--elab-run", .. common, request.Top, $"--vcd={Vcd}", $"--stop-time={request.DurationNs}ns"]);
            string path = Path.Combine(work, Vcd);
            bool ok = rc == 0 && File.Exists(path);
            return new ReferenceRun(ok, ok ? File.ReadAllText(path) : null, log.ToString(), rc);
        }
        finally
        {
            try { Directory.Delete(work, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    /// <summary>
    /// GHDL rejects a time literal without the space (<c>10ns</c>, legal in
    /// other tools) under every <c>--std</c> and has no flag to relax it, so the
    /// copy GHDL analyzes gets <c>10 ns</c>. Same meaning; the case's sources and
    /// what Kmila reads stay untouched. A number glued to an identifier
    /// (<c>clk_10ns</c>) is not a literal and is left alone.
    /// </summary>
    public static string SpaceTimeUnits(string text) => UnitWithoutSpace().Replace(text, "$1 $2");

    [GeneratedRegex(@"(?<![\w.])(\d+(?:\.\d+)?)(fs|ps|ns|us|ms|sec|min|hr)\b", RegexOptions.IgnoreCase)]
    private static partial Regex UnitWithoutSpace();

    private async Task<int> StepAsync(StringBuilder log, string work, CancellationToken ct, string[] args)
    {
        var (rc, output) = await ExecAsync(args, work, ct);
        log.AppendLine("$ ghdl " + string.Join(' ', args)).AppendLine(output);
        return rc;
    }

    private async Task<(int Rc, string Output)> ExecAsync(IEnumerable<string> args, string workDir, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ghdl,
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var stdout = p.StandardOutput.ReadToEndAsync(ct);
        var stderr = p.StandardError.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        return (p.ExitCode, await stdout + await stderr);
    }
}
