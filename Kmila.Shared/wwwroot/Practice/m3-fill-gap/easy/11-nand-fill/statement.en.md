## NAND gate — fill the gap

The entity, ports and architecture skeleton are already in place.
Implement the **single concurrent assignment** that drives `y` with
the logical NAND of `a` and `b` (the inverted AND).

### Truth table

| `a` | `b` | `y` |
|:---:|:---:|:---:|
|  0  |  0  |  1  |
|  0  |  1  |  1  |
|  1  |  0  |  1  |
|  1  |  1  |  0  |

### Where to write

Only the line between `-- BEGIN_EDIT` and `-- END_EDIT` is graded.
Everything outside those markers is locked scaffolding.

### Hint

One operator: `nand`.
