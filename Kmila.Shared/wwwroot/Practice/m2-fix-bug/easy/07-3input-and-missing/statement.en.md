## 3-input AND — find and fix the bug

The author was asked to write a 3-input AND gate, but the
current code only ANDs `a` and `b`. The third input `c` is being
ignored.

Find the bug and bring `c` back into the expression.

### Specification

* `y = a ∧ b ∧ c`
* `y = 1` only when all three inputs are `1`.

### Hint

One operand is missing on the right-hand side of `<=`.
