## ALU — fix the signed-overflow flag

The 8-bit ALU performs `a + b` in two's complement and asserts `ovf`
when the signed result cannot fit in 8 bits. The current
implementation is "almost right" — it has the same shape as the
textbook formula but is reading the wrong bit.

### Specification

* Inputs `a`, `b` are 8-bit signed (two's complement).
* Let `sum = a + b` (truncated to 8 bits).
* Signed overflow occurs **iff** both operands have the same sign and
  the result has the opposite sign.
* Formal: `ovf = (a[7] = b[7]) AND (sum[7] /= a[7])`.

### Hint

Bit 7 is the sign bit on an 8-bit two's-complement value. Reading
any other bit makes the formula mathematically meaningless.
