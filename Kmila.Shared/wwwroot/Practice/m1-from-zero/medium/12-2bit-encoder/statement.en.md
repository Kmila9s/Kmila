## 4-to-2 priority encoder

Encode the index of the highest active bit of `d` into the 2-bit output `y`. Higher-indexed bits take priority over lower ones.

| `d` | `y` |
|:---:|:---:|
| `1xxx` | 11 |
| `01xx` | 10 |
| `001x` | 01 |
| `0001` | 00 |
| `0000` | 00 |

### Hint

Chained `when ... else` from the highest priority down to the lowest gives the priority semantics for free.
