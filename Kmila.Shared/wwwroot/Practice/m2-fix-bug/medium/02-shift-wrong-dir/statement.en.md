## 8-bit shift register — wrong direction

A serial-in / parallel-out shift register was requested, shifting **left** every clock.

### Specification

* On `rising_edge(clk)`: `r <- r(6 downto 0) & din` — `din` enters at bit 0, the MSB is shifted out.
* Async active-high `reset` clears `r` to all zeros.
* `q` mirrors `r`.

### What's wrong

The author's concatenation is reversed: `din` is being inserted at the MSB end, so after a few cycles with `din = '1'` the high bits fill first instead of the low bits.

### Hint

In `r <= a & b`, the **leftmost** operand becomes the MSBs of the result. To shift left, drop the MSB and put `din` on the right.
