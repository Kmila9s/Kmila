## FIFO — fix the pointer wrap

A depth-4 FIFO is dropping data after the buffer rolls over. The
`count`/`full`/`empty` logic looks fine, but the read and write
pointers wrap one slot too late, so the FIFO behaves as if it has
five entries when it actually has four.

### Specification

* Synchronous depth-4 FIFO with active-high reset.
* Pointers occupy values `0..3` and **must wrap to 0 after 3**.
* `full = '1'` when `count = 4`, `empty = '1'` when `count = 0`.
* Pushes are ignored while full, pops while empty.

### Hint

If `DEPTH = 4`, the legal pointer range is `0..DEPTH-1`. Wrapping
on equality with `DEPTH` (or worse, `DEPTH+1`) gives one extra
phantom slot.
