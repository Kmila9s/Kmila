## AND vs OR — find and fix the bug

The author was asked to implement a 2-input AND gate, but `y`
goes high when **only one** of the inputs is `1`. The wrong
operator was used.

Find the bug and replace the operator.

### Specification

* `y = a ∧ b`
* `y = 1` only when both `a` and `b` are `1`.

### Hint

There are two basic operators here that are easy to swap.
