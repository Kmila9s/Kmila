## CRC-8 polynomial fix

⚠ 翻訳待ち — 説明は英語で表示されます。

A serial CRC-8 generator XORs a feedback bit into shift-register
positions chosen by the polynomial. The current code uses the
wrong polynomial: `0x07 = x^2 + x + 1` instead of the intended
`0x1D = x^4 + x^3 + x^2 + 1`.

### Specification

* Synchronous, active-high reset; `crc` resets to `00000000`.
* Each cycle: `fb = reg(7) xor din`.
* `reg(0) <= fb`, and for `0x1D`:
  * `reg(2) <= reg(1) xor fb`
  * `reg(3) <= reg(2) xor fb`
  * `reg(4) <= reg(3) xor fb`
  * remaining bits shift through.

### Hint

XOR taps follow the coefficients of `0x1D` (`00011101`). The buggy
version XORs at positions 1 and 2 only, matching `0x07`.
