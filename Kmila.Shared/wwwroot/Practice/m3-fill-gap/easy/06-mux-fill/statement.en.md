## 2-to-1 multiplexer — fill the gap

A 2-to-1 multiplexer routes one of two inputs to the output based
on the selector bit `sel`.

* When `sel = '0'` the output `y` follows `a`.
* When `sel = '1'` the output `y` follows `b`.

The entity, ports and architecture skeleton are already in place.
Implement the **single concurrent assignment** with a `when ... else`
expression.

### Truth table

| `sel` | `a` | `b` | `y` |
|:-----:|:---:|:---:|:---:|
|   0   |  0  |  X  |  0  |
|   0   |  1  |  X  |  1  |
|   1   |  X  |  0  |  0  |
|   1   |  X  |  1  |  1  |

### Where to write

Only the line between `-- BEGIN_EDIT` and `-- END_EDIT` is graded.
Everything outside those markers is locked scaffolding.
