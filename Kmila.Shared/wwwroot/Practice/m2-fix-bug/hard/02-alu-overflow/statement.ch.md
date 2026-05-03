## ⚠ Translation pending — Signed-overflow flag fix

The 8-bit ALU computes `a + b` in two's complement and asserts `ovf`
when the signed result does not fit in 8 bits. The current
implementation is "almost right" — same shape as the textbook
formula, but it reads the wrong bit.

### Specification

* `a`, `b` are 8-bit signed values (two's complement).
* Let `sum = a + b` (truncated to 8 bits).
* Signed overflow happens **iff** both operands share the same sign
  and the result has the opposite sign.
* Formal: `ovf = (a[7] = b[7]) AND (sum[7] /= a[7])`.

### Hint

Bit 7 is the sign bit in 8-bit two's-complement. Reading any other
bit makes the formula mathematically meaningless.
