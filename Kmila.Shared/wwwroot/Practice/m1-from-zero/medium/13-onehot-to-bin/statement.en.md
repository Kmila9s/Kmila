## One-hot to binary

Convert a 4-bit one-hot input `oh` to its 2-bit binary index `b`. Exactly one bit of `oh` is `1`.

| `oh` | `b` |
|:---:|:---:|
| 0001 | 00 |
| 0010 | 01 |
| 0100 | 10 |
| 1000 | 11 |

### Hint

A simple `when ... else` cascade does it. The output is the position of the single `1`.
