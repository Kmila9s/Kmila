## CRC-8 — fix the polynomial

This serial CRC-8 generator XORs a feedback bit into specific
shift-register positions chosen by the polynomial. The current
code wires the wrong polynomial: `0x07 = x^2 + x + 1` instead of
the intended `0x1D = x^4 + x^3 + x^2 + 1`. CRCs computed by the
buggy version will not match the expected checksum.

### Specification

* Synchronous, active-high reset; `crc` resets to `00000000`.
* Each clock cycle: `fb = reg(7) xor din`.
* `reg(0) <= fb`, then for the polynomial `0x1D`:
  * `reg(2) <= reg(1) xor fb`
  * `reg(3) <= reg(2) xor fb`
  * `reg(4) <= reg(3) xor fb`
  * remaining bits shift through unchanged.

### Hint

The XOR taps follow the polynomial coefficients of `0x1D` (binary
`00011101`). The buggy version XORs at positions 1 and 2 only,
matching `0x07`.
