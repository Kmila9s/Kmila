## LFSR — fix the polynomial

This 8-bit LFSR is supposed to generate a maximal-length pseudo
random sequence (period 255). The current code uses a non-primitive
polynomial: only two taps are XOR'd, so the register quickly
collapses into a short cycle and the output is far from random.

### Specification

* Synchronous, active-high reset; reset value `00000001`.
* Fibonacci LFSR shifting **left** with new bit fed into bit 0:
  `reg <= reg(6 downto 0) & fb`.
* The feedback `fb` is the XOR of taps **8, 6, 5, 4** (in 1-indexed
  notation), i.e. `reg(7) xor reg(5) xor reg(4) xor reg(3)`.
  Polynomial: `x^8 + x^6 + x^5 + x^4 + 1`.

### Hint

Two-tap polynomials over GF(2) are almost never primitive at
length 8. The textbook tap set for an 8-bit maximal LFSR is
`{8, 6, 5, 4}`.
