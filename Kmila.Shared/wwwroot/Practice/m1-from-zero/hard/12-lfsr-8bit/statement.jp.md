## 8 ビット LFSR

⚠ 翻訳待ち — 説明は英語で表示されます。

## 8-bit LFSR

Implement a maximal-length 8-bit Linear Feedback Shift Register with taps at positions 8, 6, 5, 4 (Fibonacci form).

### Spec

- Internal register `r : STD_LOGIC_VECTOR(7 downto 0)`, seed `"11111111"` on reset.
- On each rising clock:
  - `fb := r(7) xor r(5) xor r(4) xor r(3);`
  - `r <= r(6 downto 0) & fb;`
- Drive `q <= r`.
