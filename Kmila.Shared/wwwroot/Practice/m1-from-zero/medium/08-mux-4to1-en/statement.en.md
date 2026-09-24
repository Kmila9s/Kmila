## 4-to-1 mux with enable

Implement a 4-to-1 multiplexer with an active-high enable. When `en = '0'`, the output is forced to `0`. When `en = '1'`, the output `y` follows one of the inputs:

| `sel` | `y` |
|:---:|:---:|
| 00 | a |
| 01 | b |
| 10 | c |
| 11 | d |

### Hint

A single conditional concurrent assignment with `when ... else` chains works well.
