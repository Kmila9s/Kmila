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
// This is read-only at the moment — output widgets can consume a slice
// of a multi-bit OUT port, but input widgets (Switch, Button) still
// drive whole ports. Write-side merging (multiple inputs claiming the
// same port at different bits) is deferred — input-side fan-in needs
// coordinator-level overlay logic that's a bigger lift.
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
        int open = s.IndexOf('[');
        if (open < 0) return (s, null);
        int close = s.IndexOf(']', open + 1);
        if (close <= open) return (s, null);
        string name = s.Substring(0, open).Trim();
        string spec = s.Substring(open + 1, close - open - 1).Trim();
        if (spec.Length == 0) return (name, null);
        int colon = spec.IndexOf(':');
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
}

/// <summary>
/// LSB-numbered inclusive bit-range. High >= Low; single-bit slices set
/// both fields equal. Indices are in std_logic_vector convention (bit 0 =
/// LSB = rightmost in the string repr).
/// </summary>
public readonly record struct BitSlice(int High, int Low)
{
    public int Width => High - Low + 1;
}
