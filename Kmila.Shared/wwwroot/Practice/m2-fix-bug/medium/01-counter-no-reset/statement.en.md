## 4-bit counter — async reset bug

The block is meant to be a 4-bit counter with an **asynchronous active-high reset**.

### Specification

* On `rising_edge(clk)`: `count <- count + 1`.
* When `reset = '1'`: `count` is forced to `"0000"` immediately, regardless of the clock.
* `q` mirrors `count`.

### What's wrong

If you pulse `reset` between two rising edges, the count does **not** clear right away — it only clears on the next clock edge. The body of the process is fine; the bug is one line above it.

### Hint

For a truly asynchronous reset, the reset signal must appear in the process **sensitivity list** alongside the clock.
