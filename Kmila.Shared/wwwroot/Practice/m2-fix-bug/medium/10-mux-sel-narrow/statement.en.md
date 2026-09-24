## 4-to-1 multiplexer — selector under-decoded

A 4-to-1 mux was specified, with `sel` of width 2:

| `sel` | `y` |
|:-----:|:---:|
| `00`  | `a` |
| `01`  | `b` |
| `10`  | `c` |
| `11`  | `d` |

### What's wrong

The author only checked `sel(0)`, so the case statement collapses to a 2-to-1 mux that only routes `a` (when `sel(0) = '0'`) or `b` (when `sel(0) = '1'`). Inputs `c` and `d` are **never reachable**.

### Hint

Use the **whole** `sel` vector in the `case` and provide one branch per binary value `"00"`, `"01"`, `"10"`, `"11"`.
