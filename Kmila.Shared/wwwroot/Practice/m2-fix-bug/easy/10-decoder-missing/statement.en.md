## 2-to-4 decoder — find and fix the bug

A 2-to-4 decoder activates exactly **one** of the four output
bits according to `sel`. Three of the four cases are handled
correctly, but one case has been forgotten and falls through to
the default `"0000"`.

Find the missing case and add it.

### Specification

| `sel` | `y`     |
|:-----:|:-------:|
| 00    | 0001    |
| 01    | 0010    |
| 10    | 0100    |
| 11    | 1000    |

### Hint

Check which value of `sel` does not appear among the `when`
clauses.
