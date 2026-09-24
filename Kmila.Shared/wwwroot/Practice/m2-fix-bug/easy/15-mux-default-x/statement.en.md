## 2-to-1 MUX — wrong default branch

A 2-to-1 MUX should output `a` when `sel = 0` and `b` when
`sel = 1`. The starter routes `a` correctly when `sel = 0`, but
when `sel = 1` it drives `'1'` instead of `b` — `b` is never
read.

Find the bug and use `b` on the `else` branch.

### Specification

* `sel = 0` → `y = a`
* `sel = 1` → `y = b`

### Hint

Look at what literal sits after the `else` — it should be a
**signal**, not a constant.
