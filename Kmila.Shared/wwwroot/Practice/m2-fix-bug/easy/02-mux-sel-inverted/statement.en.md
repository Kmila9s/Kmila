## 2-to-1 MUX — find and fix the bug

A 2-to-1 multiplexer should route `a` to `y` when `sel = 0`, and
`b` to `y` when `sel = 1`. The author got the polarity of `sel`
**reversed**.

Find the bug and correct it.

### Specification

* `sel = 0` → `y = a`
* `sel = 1` → `y = b`

### Hint

Look carefully at the value of `sel` used in the `when` clause.
