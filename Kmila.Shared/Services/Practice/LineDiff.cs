using System.Text;

namespace Kmila.Shared.Services.Practice;

/// <summary>
/// Pure C# line-level unified-diff helper. Used by the practice-session
/// export to show what changed between consecutive attempts. No external
/// dependency: the longest-common-subsequence table is small enough for
/// VHDL files (a few hundred lines max).
/// </summary>
internal static class LineDiff
{
    /// <summary>
    /// Returns a unified diff (`@@` hunks, `-`/`+` lines) comparing
    /// <paramref name="oldText"/> against <paramref name="newText"/>.
    /// Returns an empty string when the inputs are identical so callers
    /// can decide to omit the section entirely.
    /// </summary>
    public static string UnifiedDiff(string oldText, string newText, string oldLabel, string newLabel, int context = 3)
    {
        if (string.Equals(oldText, newText, StringComparison.Ordinal)) return string.Empty;

        var oldLines = SplitLines(oldText);
        var newLines = SplitLines(newText);

        var ops = LcsOps(oldLines, newLines);
        if (ops.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.Append("--- ").Append(oldLabel).Append('\n');
        sb.Append("+++ ").Append(newLabel).Append('\n');

        // Group ops into hunks separated by ≥ 2*context unchanged lines.
        int i = 0;
        while (i < ops.Count)
        {
            // Skip leading equals beyond context.
            while (i < ops.Count && ops[i].Kind == OpKind.Equal) i++;
            if (i >= ops.Count) break;

            int hunkStart = Math.Max(0, i - context);
            int j = i;
            while (j < ops.Count)
            {
                if (ops[j].Kind != OpKind.Equal) { j++; continue; }
                // count run of equals
                int runStart = j;
                while (j < ops.Count && ops[j].Kind == OpKind.Equal) j++;
                int runLen = j - runStart;
                if (j >= ops.Count || runLen >= context * 2)
                {
                    j = runStart + Math.Min(runLen, context);
                    break;
                }
            }
            int hunkEnd = Math.Min(ops.Count, j);

            int oldStart = ops[hunkStart].OldIndex;
            int newStart = ops[hunkStart].NewIndex;
            int oldCount = 0, newCount = 0;
            for (int k = hunkStart; k < hunkEnd; k++)
            {
                if (ops[k].Kind != OpKind.Insert) oldCount++;
                if (ops[k].Kind != OpKind.Delete) newCount++;
            }

            sb.Append("@@ -").Append(oldStart + 1).Append(',').Append(oldCount)
              .Append(" +").Append(newStart + 1).Append(',').Append(newCount).Append(" @@\n");

            for (int k = hunkStart; k < hunkEnd; k++)
            {
                var op = ops[k];
                char prefix = op.Kind switch { OpKind.Equal => ' ', OpKind.Delete => '-', OpKind.Insert => '+', _ => ' ' };
                sb.Append(prefix).Append(op.Line).Append('\n');
            }

            i = hunkEnd;
        }

        return sb.ToString();
    }

    private enum OpKind { Equal, Delete, Insert }
    private readonly record struct DiffOp(OpKind Kind, string Line, int OldIndex, int NewIndex);

    private static List<DiffOp> LcsOps(string[] a, string[] b)
    {
        int n = a.Length, m = b.Length;
        // 2-D table of LCS lengths; sizes are bounded by VHDL files which are tiny.
        var dp = new int[n + 1, m + 1];
        for (int x = n - 1; x >= 0; x--)
            for (int y = m - 1; y >= 0; y--)
                dp[x, y] = string.Equals(a[x], b[y], StringComparison.Ordinal)
                    ? dp[x + 1, y + 1] + 1
                    : Math.Max(dp[x + 1, y], dp[x, y + 1]);

        var ops = new List<DiffOp>(n + m);
        int i = 0, j = 0;
        while (i < n && j < m)
        {
            if (string.Equals(a[i], b[j], StringComparison.Ordinal))
            {
                ops.Add(new DiffOp(OpKind.Equal, a[i], i, j));
                i++; j++;
            }
            else if (dp[i + 1, j] >= dp[i, j + 1])
            {
                ops.Add(new DiffOp(OpKind.Delete, a[i], i, j));
                i++;
            }
            else
            {
                ops.Add(new DiffOp(OpKind.Insert, b[j], i, j));
                j++;
            }
        }
        while (i < n) { ops.Add(new DiffOp(OpKind.Delete, a[i], i, j)); i++; }
        while (j < m) { ops.Add(new DiffOp(OpKind.Insert, b[j], i, j)); j++; }
        return ops;
    }

    private static string[] SplitLines(string text)
    {
        if (text.Length == 0) return Array.Empty<string>();
        return text.Replace("\r\n", "\n", System.StringComparison.Ordinal).Split('\n');
    }
}
