## SPI — fix the sample edge

This is a bare-bones SPI master clock generator. It divides `clk`
by 8 to produce `sclk` (toggling at divider counts 3 and 7) and
pulses `sample` for one cycle when `miso` should be latched. The
bug: `sample` is currently asserted on the **rising** edge of
`sclk`, but SPI mode 0 says the slave drives `miso` on rising
and the master must sample on **falling**.

### Specification

* `sclk` toggles: rises at `divider = 3`, falls at `divider = 7`.
* `sample` is a one-cycle pulse equal to `miso` **at the falling
  edge of `sclk`**.
* Active-high synchronous reset clears everything to 0.

### Hint

Move the `sample_int <= miso;` assignment from the `divider = 3`
branch to the `divider = 7` branch.
