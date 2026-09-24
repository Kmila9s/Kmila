## Half-adder — fill in the sum

A half-adder takes two 1-bit inputs `a` and `b` and produces a
2-bit result: a sum bit and a carry-out bit.

The **carry-out** line `cout <= a and b;` is already provided
outside the editable region. Your job is to write the **sum**
assignment.

### Behavior

* `cout <= a and b`  *(given)*
* `sum  <= a xor b`  *(write this)*

### Truth table

| `a` | `b` | `sum` | `cout` |
|:---:|:---:|:-----:|:------:|
|  0  |  0  |   0   |   0    |
|  0  |  1  |   1   |   0    |
|  1  |  0  |   1   |   0    |
|  1  |  1  |   0   |   1    |

### Where to write

Only the line between `-- BEGIN_EDIT` and `-- END_EDIT` is graded.
The `cout` assignment outside the markers must not be touched.
