## ⚠ Translation pending — Asymmetric sign-extension fix

Two unsigned 4-bit values `a` and `b` are multiplied to produce an
8-bit unsigned `product`. The current code accidentally interprets
`a` as **signed** while keeping `b` unsigned, which gives the wrong
answer whenever `a(3) = '1'` (i.e. `a >= 8`).

### Specification

* `a`, `b` are 4-bit unsigned integers in `0..15`.
* `product = a * b` interpreted as 8-bit unsigned (range `0..225`).

### Hint

Cast both operands the same way: `unsigned(a) * unsigned(b)`. The
result already fits exactly in 8 bits — no resize required.
