## Full-adder — find and fix the bug

The author was asked to implement a 1-bit full-adder. The `sum`
output is correct, but the `cout` output is **wrong** for at least
one input combination.

Find the bug in the architecture and fix it.

### Specification

For inputs `a`, `b`, `cin` ∈ {0, 1}:

* `sum  = a ⊕ b ⊕ cin`
* `cout = (a ∧ b) ∨ (cin ∧ (a ⊕ b))`

### Hint

The carry-out must account for `cin` — not just `a` and `b`.
