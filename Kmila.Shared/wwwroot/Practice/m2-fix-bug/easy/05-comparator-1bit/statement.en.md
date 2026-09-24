## 4-bit equality — find and fix the bug

This circuit should output `eq = 1` when the 4-bit vectors `a`
and `b` are equal, and `0` otherwise. It works for many cases —
but reports "equal" for some pairs that clearly are not.

Find the bug and fix it.

### Specification

* `eq = 1` when `a = b`, else `0`.
* All four bits must take part in the comparison.

### Hint

Check the index range used in the comparison.
