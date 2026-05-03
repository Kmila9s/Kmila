## ⚠ ترجمة قيد الإنجاز — 4-bit counter, async reset bug

The block is meant to be a 4-bit counter with an **asynchronous active-high reset**.

* On `rising_edge(clk)`: `count <- count + 1`.
* When `reset = '1'`: `count` is forced to `"0000"` immediately.
* `q` mirrors `count`.

If you pulse `reset` between two rising edges, the count does **not** clear right away. The body of the process is fine; the bug is one line above it.

**Hint:** for a truly asynchronous reset, the reset signal must appear in the process **sensitivity list** next to the clock.
