## NOR gate — find and fix the bug

The author was asked to implement a 2-input NOR gate. The
expression `a nor b` was almost there, but one of the operands
got an unwanted `not` applied to it, which breaks the truth
table.

Find the spurious inversion and remove it.

### Specification

| `a` | `b` | `y` |
|:---:|:---:|:---:|
|  0  |  0  |  1  |
|  0  |  1  |  0  |
|  1  |  0  |  0  |
|  1  |  1  |  0  |

### Hint

Both inputs of a NOR feed in directly — without any extra
inverter.
