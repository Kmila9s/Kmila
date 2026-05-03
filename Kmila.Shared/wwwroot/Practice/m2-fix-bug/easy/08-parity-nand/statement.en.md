## 4-bit parity — find and fix the bug

This circuit should output the **parity** of the 4-bit vector
`d`: `p = 1` when an odd number of bits in `d` are `1`, `p = 0`
otherwise. The author tried to chain NAND gates instead — and
got it wrong.

Find the bug and use the right operator.

### Specification

* `p = d(0) ⊕ d(1) ⊕ d(2) ⊕ d(3)`

### Hint

Parity is exactly what XOR computes — chained NANDs do not.
