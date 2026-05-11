## 8-bit shift register, wrong direction

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

A serial-in / parallel-out shift register was requested, shifting **left** every clock.

* On `rising_edge(clk)`: `r <- r(6 downto 0) & din`. `din` enters at bit 0, the MSB falls off.
* Async high-active reset clears `r`.
* `q` mirrors `r`.

The author's concatenation is reversed: `din` is inserted at the MSB side, so the high bits fill first.

**Hint:** in `r <= a & b`, the leftmost operand is the MSB. To shift left, drop the MSB and put `din` on the right.
