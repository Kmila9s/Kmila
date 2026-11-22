using System.Text.Json;
using Kmila.Frontend;
using Kmila.Shared.Models;

namespace Kmila.TestKit.Cases;

/// <summary>A case on disk: its folder, spec and one variant per HDL language it ships.</summary>
public sealed record LoadedCase(string Dir, CaseSpec Spec, HdlVariants<CaseVariant> Variants);

/// <summary>Reads <c>Cases/&lt;id&gt;/case.json</c> (content schema v2) and every <c>variants/&lt;lang&gt;/variant.json</c>.</summary>
public static class CaseLoader
{
    /// <summary>Every case folder under <paramref name="casesDir"/> that has a <c>case.json</c>, ordered by id.</summary>
    public static IEnumerable<LoadedCase> LoadAll(string casesDir) =>
        Directory.EnumerateDirectories(casesDir)
            .Where(d => File.Exists(Path.Combine(d, "case.json")))
            .Order(StringComparer.Ordinal)
            .Select(Load);

    /// <summary>
    /// Loads one case. A <c>case.json</c> of another schema version is an error
    /// (never half-read); a variant whose language is not enabled yet is ignored,
    /// as in every v2 content file.
    /// </summary>
    public static LoadedCase Load(string caseDir)
    {
        string specFile = Path.Combine(caseDir, "case.json");
        var spec = JsonSerializer.Deserialize<CaseSpec>(File.ReadAllText(specFile))
            ?? throw new InvalidDataException($"{specFile}: empty");
        if (spec.Version != ContentSchema.Version)
            throw new InvalidDataException($"{specFile}: schema v{spec.Version}; this build reads v{ContentSchema.Version}");

        var raw = new Dictionary<string, CaseVariant>(StringComparer.OrdinalIgnoreCase);
        string root = Path.Combine(caseDir, "variants");
        if (Directory.Exists(root))
        {
            foreach (var dir in Directory.EnumerateDirectories(root).Order(StringComparer.Ordinal))
            {
                string file = Path.Combine(dir, "variant.json");
                if (!File.Exists(file)) continue;
                var v = JsonSerializer.Deserialize<CaseVariant>(File.ReadAllText(file))
                    ?? throw new InvalidDataException($"{file}: empty");
                if (HdlLanguages.FromId(v.LanguageId) is not { } language) continue;
                v.Dir = dir;
                v.Language = language;
                v.SourcePaths = v.Sources.Select(s => Path.Combine(dir, "reference", s)).ToList();
                raw[v.LanguageId] = v;
            }
        }
        return new LoadedCase(caseDir, spec, HdlVariants<CaseVariant>.FromIds(raw));
    }
}
