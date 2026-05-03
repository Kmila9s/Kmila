## 4-bit even parity — fill the gap

The even-parity bit `p` of a 4-bit word `d` is `1` when the number
of `1` bits in `d` is **odd**, and `0` when it is even — exactly the
XOR of the four input bits.

### Behavior

`p <= d(0) xor d(1) xor d(2) xor d(3)`

### Examples

| `d`   | ones | `p` |
|:------|:----:|:---:|
| 0000  |  0   |  0  |
| 0001  |  1   |  1  |
| 0011  |  2   |  0  |
| 1110  |  3   |  1  |
| 1111  |  4   |  0  |

### Where to write

The entity, ports and architecture skeleton are already in place.
Only the line between `-- BEGIN_EDIT` and `-- END_EDIT` is graded —
everything outside is locked scaffolding.
