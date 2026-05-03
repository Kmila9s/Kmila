## Half-adder — fill in the carry-out

A half-adder takes two 1-bit inputs `a` and `b` and produces a
2-bit result: a sum bit and a carry-out bit.

The **sum** line `sum <= a xor b;` is already provided outside the
editable region. Your job is to write the **carry-out** assignment.

### Behavior

* `sum  <= a xor b`  *(given)*
* `cout <= a and b`  *(write this)*

### Truth table

| `a` | `b` | `sum` | `cout` |
|:---:|:---:|:-----:|:------:|
|  0  |  0  |   0   |   0    |
|  0  |  1  |   1   |   0    |
|  1  |  0  |   1   |   0    |
|  1  |  1  |   0   |   1    |

### Where to write

Only the line between `-- BEGIN_EDIT` and `-- END_EDIT` is graded.
The `sum` assignment outside the markers must not be touched.
