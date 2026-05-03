## 2-to-1 MUX — inputs swapped

A 2-to-1 MUX should select `a` when `sel = 0` and `b` when
`sel = 1`. Here the **data inputs** are swapped: `b` is selected
on `sel = 0` and `a` on `sel = 1`.

Find the bug and put each input on the correct branch.

### Specification

* `sel = 0` → `y = a`
* `sel = 1` → `y = b`

### Hint

The polarity of `sel` is fine — but the operands of the
expression are not.
