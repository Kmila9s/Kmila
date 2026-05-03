## XOR gate — fill the gap

The entity, ports and architecture skeleton are already in place.
Implement the **single concurrent assignment** that drives `y` with
the logical exclusive OR of `a` and `b` (true when the inputs
differ).

### Truth table

| `a` | `b` | `y` |
|:---:|:---:|:---:|
|  0  |  0  |  0  |
|  0  |  1  |  1  |
|  1  |  0  |  1  |
|  1  |  1  |  0  |

### Where to write

Only the line between `-- BEGIN_EDIT` and `-- END_EDIT` is graded.
Everything outside those markers is locked scaffolding.

### Hint

One operator: `xor`.
