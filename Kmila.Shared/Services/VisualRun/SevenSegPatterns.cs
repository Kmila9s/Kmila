namespace Kmila.Shared.Services.VisualRun;

// Shared 7-segment decode tables + helpers, used by both the single-digit
// SevenSegmentWidget and the multi-digit SevenSegmentBankWidget so the decode
// logic lives in exactly one place.
//
// A "pattern" is an int whose low 7 bits say which segments glow, bit order
// g f e d c b a (low→high) — i.e. bit 0 = segment a, bit 6 = segment g.
// E.g. '0' = abcdef on, g off → 0b0111111 = 0x3F.
public static class SevenSegPatterns
{
    // Canonical segment patterns for 0..F (see bit-order note above).
    public static readonly int[] HexPatterns = new[]
    {
        0x3F, // 0
        0x06, // 1
        0x5B, // 2
        0x4F, // 3
        0x66, // 4
        0x6D, // 5
        0x7D, // 6
        0x07, // 7
        0x7F, // 8
        0x6F, // 9
        0x77, // A
        0x7C, // b
        0x39, // C
        0x5E, // d
        0x79, // E
        0x71, // F
    };

    // Decode a nibble's bit string (MSB-first, e.g. "1010") in "hex" or "dec"
    // mode. Returns the numeric value 0..15, or -1 to mean "blank the digit":
    // any non-0/1 char (U/X/Z) blanks, and in "dec" mode a value > 9 blanks
    // too (matching what a real BCD decoder IC does on out-of-range inputs).
    public static int DecodeNibble(string? bits4, string mode)
    {
        if (string.IsNullOrEmpty(bits4)) return -1;
        string s = bits4.Length >= 4 ? bits4[^4..] : bits4.PadLeft(4, '0');
        int n = 0;
        for (int i = 0; i < 4; i++)
        {
            n <<= 1;
            if (s[i] == '1') n |= 1;
            else if (s[i] != '0') return -1; // U/X/Z → blank
        }
        if (mode == "dec" && n > 9) return -1;
        return n;
    }

    // True when the given segment (a..g) is lit in `pattern`.
    public static bool SegmentLit(int pattern, char segment)
    {
        int idx = segment switch
        {
            'a' => 0,
            'b' => 1,
            'c' => 2,
            'd' => 3,
            'e' => 4,
            'f' => 5,
            'g' => 6,
            _   => -1,
        };
        if (idx < 0) return false;
        return (pattern & (1 << idx)) != 0;
    }
}
