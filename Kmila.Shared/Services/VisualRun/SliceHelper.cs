namespace Kmila.Shared.Services.VisualRun;

// Phase 3++ (2026-06-26): bit-slice support in widget bindings.
//
// Binding-value syntax extended from a bare port name to optionally
// include a slice spec:
//
//   "count"       → whole port (existing behavior, no slice)
//   "count[3]"    → single bit (LSB-numbered, like VHDL std_logic_vector)
//   "count[3:0]"  → bit range (MSB-first, matching VHDL "downto" ranges)
//
// The slice operates on the port's *string representation* — which the
// engine emits MSB-on-left for std_logic_vector(N-1 downto 0), so e.g. a
// 4-bit value 0xA renders as "1010" (bit3 bit2 bit1 bit0). A slice of
// "[2]" picks position 2 from the right = '0', "[3:1]" picks "101".
//
// Read side: output widgets consume a slice of a multi-bit OUT port
// via <see cref="Extract"/>.
// Write side (Ring 5, 2026-07-14): input widgets can now bind their
// Out pins to a slice of a multi-bit IN port. The coordinator stores
// per-slice contributions and composes them via <see cref="Compose"/>
// at drain time. Disjoint slices from different widgets pack into one
// port cleanly; overlaps behave as last-writer-wins on the overlap
// bits (surface as a wiring warning in the modal, not enforced here).
/// <summary>Helpers for parsing, extracting, and composing bit-slice port bindings.</summary>
public static class SliceHelper
{
    /// <summary>
    /// Parses a binding value like "count[3]" or "bus[7:0]" into the bare
    /// port name + an optional slice descriptor. Returns null slice when
    /// the input has no bracket suffix.
    /// </summary>
    /// <param name="binding">A binding value as stored in .kviz JSON.</param>
    /// <returns>
    /// Tuple of (portName, slice). slice is null for whole-port bindings;
    /// otherwise carries the parsed high/low indices (equal when the user
    /// picked a single bit like "[3]"). Returns ("", null) on null/empty
    /// inputs so callers don't need to null-check.
    /// </returns>
    public static (string portName, BitSlice? slice) Parse(string? binding)
    {
        if (string.IsNullOrWhiteSpace(binding)) return ("", null);
        var s = binding.Trim();
        int open = s.IndexOf('[', System.StringComparison.Ordinal);
        if (open < 0) return (s, null);
        int close = s.IndexOf(']', open + 1);
        if (close <= open) return (s, null);
        string name = s.Substring(0, open).Trim();
        string spec = s.Substring(open + 1, close - open - 1).Trim();
        if (spec.Length == 0) return (name, null);
        int colon = spec.IndexOf(':', System.StringComparison.Ordinal);
        if (colon < 0)
        {
            // Single bit: [N]
            if (!int.TryParse(spec, out var bit) || bit < 0) return (name, null);
            return (name, new BitSlice(bit, bit));
        }
        // Range: [H:L]
        var hs = spec.Substring(0, colon).Trim();
        var ls = spec.Substring(colon + 1).Trim();
        if (!int.TryParse(hs, out var hi) || !int.TryParse(ls, out var lo)) return (name, null);
        if (hi < 0 || lo < 0) return (name, null);
        // Tolerate reversed order — student writes "[0:3]" instead of "[3:0]".
        if (lo > hi) (hi, lo) = (lo, hi);
        return (name, new BitSlice(hi, lo));
    }

    /// <summary>
    /// Given a port's full string value (MSB-on-left, e.g. "1010") and an
    /// optional slice, returns the relevant substring (or the whole value
    /// when slice is null). Out-of-range slices return empty string; the
    /// widget treats that as "blank / unwired".
    /// </summary>
    public static string Extract(string fullValue, BitSlice? slice)
    {
        if (string.IsNullOrEmpty(fullValue)) return string.Empty;
        if (slice is null) return fullValue;
        int width = fullValue.Length;
        // Bit index N (LSB-numbered) → string position from the right.
        // width = 4, bit 0 → position width-1 = 3 (the rightmost char).
        int hi = slice.Value.High;
        int lo = slice.Value.Low;
        if (lo >= width) return string.Empty;
        if (hi >= width) hi = width - 1;
        // Slice substring inclusive: from position (width-1-hi) to (width-1-lo)
        int startPos = width - 1 - hi;
        int endPos = width - 1 - lo;
        if (startPos < 0) startPos = 0;
        if (endPos < startPos) return string.Empty;
        return fullValue.Substring(startPos, endPos - startPos + 1);
    }

    /// <summary>Convenience: render a slice back to canonical "[H:L]" or "[N]" text.</summary>
    public static string Format(BitSlice slice)
        => slice.High == slice.Low ? $"[{slice.High}]" : $"[{slice.High}:{slice.Low}]";

    /// <summary>
    /// Assembles a full port bit-string (MSB on left, <paramref name="portWidth"/>
    /// chars) from a set of per-slice contributions. Undriven bits default
    /// to '0'. Overlapping slices resolve by dictionary iteration order —
    /// callers that need strict enforcement should validate via
    /// <see cref="DetectOverlap"/> first.
    /// </summary>
    /// <param name="portWidth">Width of the target packed vector in bits.</param>
    /// <param name="contributions">Slice → value pairs. Value is treated as MSB-on-left.</param>
    public static string Compose(int portWidth, IEnumerable<KeyValuePair<BitSlice, string>> contributions)
    {
        if (portWidth <= 0) return string.Empty;
        var buf = new char[portWidth];
        for (int i = 0; i < portWidth; i++) buf[i] = '0';
        foreach (var kv in contributions)
        {
            var slice = kv.Key;
            var raw = kv.Value ?? string.Empty;
            int sw = slice.Width;
            if (sw <= 0) continue;
            // Normalize the contribution to the slice's width: shorter → pad
            // with leading '0'; longer → keep the trailing sw chars.
            string norm = raw.Length == sw
                ? raw
                : raw.Length < sw
                    ? raw.PadLeft(sw, '0')
                    : raw[^sw..];
            // Slice bits are LSB-numbered. norm[0] is the MSB char (slice.High);
            // string layout is MSB-on-left so bit N maps to char index
            // (portWidth - 1 - N).
            for (int b = 0; b < sw; b++)
            {
                int bit = slice.High - b;
                if (bit < 0 || bit >= portWidth) continue;
                int pos = portWidth - 1 - bit;
                buf[pos] = norm[b];
            }
        }
        return new string(buf);
    }

    /// <summary>
    /// Reports overlapping bit ranges within a set of slices. Returns the
    /// list of colliding (a, b) pairs where a.High/Low intersects b.High/Low.
    /// Callers use this to raise wiring warnings in the modal.
    /// </summary>
    public static IReadOnlyList<(BitSlice a, BitSlice b)> DetectOverlap(IReadOnlyList<BitSlice> slices)
    {
        var collisions = new List<(BitSlice, BitSlice)>();
        for (int i = 0; i < slices.Count; i++)
            for (int j = i + 1; j < slices.Count; j++)
            {
                var a = slices[i];
                var b = slices[j];
                int loMax = Math.Max(a.Low, b.Low);
                int hiMin = Math.Min(a.High, b.High);
                if (loMax <= hiMin) collisions.Add((a, b));
            }
        return collisions;
    }
}

/// <summary>
/// LSB-numbered inclusive bit-range. High >= Low; single-bit slices set
/// both fields equal. Indices are in std_logic_vector convention (bit 0 =
/// LSB = rightmost in the string repr).
/// </summary>
public readonly record struct BitSlice(int High, int Low)
{
    /// <summary>Number of bits covered by the slice.</summary>
    public int Width => High - Low + 1;
}
