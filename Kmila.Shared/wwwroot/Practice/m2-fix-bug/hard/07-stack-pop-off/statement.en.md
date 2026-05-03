## Stack — fix the off-by-one `empty` flag

A simple stack reports its `empty` status one clock cycle late.
After a push, `empty` stays `'1'` for an extra cycle; after the
last pop, `empty` returns to `'1'` one cycle after `count` has
already reached 0. The downstream block sees a stale flag and
either misses the first valid item or reads from an empty stack.

### Specification

* Synchronous, active-high reset.
* `count` increments on `push` (and not `pop`), decrements on `pop`
  (and not `push`, while `count > 0`).
* `empty = '1'` exactly when `count = 0`, **without one cycle of
  latency**.

### Hint

Don't register the comparison: `empty` should be a plain
combinational view of `count`.
