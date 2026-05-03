## 4-bit equality with XOR — find and fix the bug

This circuit should output `eq = 1` when `a = b` and `0`
otherwise. The author tried to compute it with a long chain of
`xor` over individual bits — but that gives the **parity**, not
the equality.

Find the bug and use the right comparison.

### Specification

* `eq = 1` when `a = b`, else `0`.

### Hint

Equality is true when **every** corresponding bit pair matches —
not just when an odd or even number of `1`s appears.
