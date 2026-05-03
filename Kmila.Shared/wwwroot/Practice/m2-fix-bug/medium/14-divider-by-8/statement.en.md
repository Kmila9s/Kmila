## Clock divider — wrong divisor

A divide-by-4 clock was specified.

### Specification

* On every `rising_edge(clk)` increment a 3-bit counter `cnt`.
* `clk_out` is **bit 1** of the counter, so `clk_out` toggles every 2 input clocks → output period = 4 input clocks → divide by 4.
* Async active-high `reset` clears `cnt`.

### What's wrong

The starter taps `cnt(2)` instead of `cnt(1)`. That divides by **8**, not 4 — `clk_out` toggles every 4 input clocks.

### Hint

For a power-of-two divider built from a free-running counter, tapping bit `n` divides by `2^(n+1)`. Choose the bit that gives a period of 4.
