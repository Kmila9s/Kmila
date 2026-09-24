## SPI falling-edge detector — fill the gap

A common SPI front-end needs to detect the **falling edge** of
the slow `sclk` signal in the fast `clk` domain. We do this by
keeping a one-tick history register `sclk_prev`, then comparing
it with the current `sclk`.

The history register, asynchronous reset and the
`process(clk, reset)` block are already wired. You just write
the **edge expression** that produces the `sample` strobe.

### Specification

* Falling-edge condition: `sclk_prev = '1'` AND `sclk = '0'`.
* Implement it as a single concurrent assignment:
  ```vhdl
  sample <= sclk_prev and (not sclk);
  ```
* The strobe lasts for one `clk` cycle.

### Where to write

Only the line between `-- BEGIN_EDIT` and `-- END_EDIT` is
graded. Do not modify the registered process — it is locked.
