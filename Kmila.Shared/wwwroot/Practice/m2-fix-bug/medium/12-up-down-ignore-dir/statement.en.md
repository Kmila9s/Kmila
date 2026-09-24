## Up/down counter — direction ignored

A 4-bit up/down counter was specified, controlled by `dir`:

* `dir = '1'` → count up: `count <- count + 1`.
* `dir = '0'` → count down: `count <- count - 1`.
* Async active-high `reset` clears `count`.

### What's wrong

The starter only ever increments — `dir` has no effect at all. With `dir = '0'` the count still goes `0 → 1 → 2 → …` instead of wrapping `0 → 15 → 14 → …`.

### Hint

You need an `if dir = '1' then count <= count + 1; else count <= count - 1; end if;` inside the rising-edge branch.
