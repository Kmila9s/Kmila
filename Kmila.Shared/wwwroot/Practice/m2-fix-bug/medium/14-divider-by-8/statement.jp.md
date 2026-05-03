## ⚠ 翻訳作業中 — Clock divider, wrong divisor

A divide-by-4 clock. A 3-bit free-running counter `cnt` increments each rising edge; `clk_out` should be `cnt(1)` so the period is 4 input clocks.

The starter taps `cnt(2)` and divides by 8 instead.

**Hint:** tapping bit `n` divides by `2^(n+1)`. Use bit 1 for divide-by-4.
