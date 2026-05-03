## ⚠ Translation pending — LFSR primitive polynomial fix

This 8-bit LFSR should generate a maximal-length pseudo-random
sequence (period 255). The current code uses a non-primitive
polynomial — only two taps are XOR'd, so the register collapses
into a short cycle.

### Specification

* Synchronous, active-high reset; reset value `00000001`.
* Fibonacci LFSR shifting **left**, new bit fed into bit 0:
  `reg <= reg(6 downto 0) & fb`.
* Feedback `fb` is the XOR of taps **8, 6, 5, 4** (1-indexed),
  i.e. `reg(7) xor reg(5) xor reg(4) xor reg(3)`. Polynomial:
  `x^8 + x^6 + x^5 + x^4 + 1`.

### Hint

Two-tap polynomials over GF(2) are almost never primitive at
length 8. Use `{8, 6, 5, 4}`.
