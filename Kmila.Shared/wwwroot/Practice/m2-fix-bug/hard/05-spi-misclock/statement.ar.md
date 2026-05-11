## SPI sample-edge fix

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

A bare-bones SPI master clock generator divides `clk` by 8 to
produce `sclk` (toggling at divider counts 3 and 7) and pulses
`sample` for one cycle when `miso` should be latched. Bug:
`sample` is asserted on the **rising** edge of `sclk`, but SPI
mode 0 requires sampling on the **falling** edge.

### Specification

* `sclk` toggles: rises at `divider = 3`, falls at `divider = 7`.
* `sample` is a one-cycle pulse equal to `miso` **at the falling
  edge of `sclk`**.
* Active-high synchronous reset clears everything.

### Hint

Move the `sample_int <= miso;` assignment from the `divider = 3`
branch to the `divider = 7` branch.
