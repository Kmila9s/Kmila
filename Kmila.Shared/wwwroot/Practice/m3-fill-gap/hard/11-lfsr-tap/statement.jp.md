## LFSR タップ — 穴埋め

⚠ 翻訳待ち — 説明は英語で表示されます。

Implement the feedback computation and shift for an 8-bit maximal-length LFSR with taps at bit positions 8, 6, 5, 4.

```vhdl
fb := r(7) xor r(5) xor r(4) xor r(3);
r <= r(6 downto 0) & fb;
```

The register is seeded to `"11111111"` on reset. Only the lines between the markers are graded.
