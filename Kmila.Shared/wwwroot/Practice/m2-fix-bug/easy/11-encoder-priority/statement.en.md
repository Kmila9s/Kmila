## 4-to-2 priority encoder — find and fix the bug

A 4-to-2 priority encoder outputs the **index of the highest
set bit** in `d`. The author wrote a chain of `when` clauses
that scans the bits in the **wrong order**, so when several bits
are `1` the encoder returns the lowest-index bit instead of the
highest.

Find the bug and fix the priority order.

### Specification

* If `d(3) = 1` → `y = "11"`
* Else if `d(2) = 1` → `y = "10"`
* Else if `d(1) = 1` → `y = "01"`
* Else → `y = "00"`

### Hint

`when ... else` evaluates **top-down** — the first matching
clause wins.
