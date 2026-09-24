## 3-bit wrap counter — fill the gap

A 3-bit synchronous counter is **almost** complete. Reset, clock-edge
detection and the output assignment to `q` are already wired.

Implement the body inside the `rising_edge(clk)` branch so that the
counter walks `000 → 001 → … → 111 → 000`, i.e. wraps cleanly back to
zero after reaching seven.

### Specification

* Asynchronous reset: `count ← 000`
* On `rising_edge(clk)`:
  * if `count = 111` → `count ← 000`
  * else `count ← count + 1`
* `q` reflects `count` at all times.

### Where to write

Only edit between `-- BEGIN_EDIT` and `-- END_EDIT`. Everything else
is locked scaffolding the grader expects unchanged.

### Hint

Compare `count` against `to_unsigned(7, 3)` (or `"111"`) and choose
between `(others => '0')` and `count + 1`.
