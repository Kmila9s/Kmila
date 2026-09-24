## Shift register fill-bit selector — fill the gap

An 8-bit left-shift register is wired up: reset clears it, and on every
rising clock edge the register shifts and accepts a single fill bit
called `feed` on the right.

You must implement how `feed` is chosen:

* `mode = '1'` → `feed` follows `din`
* `mode = '0'` → `feed` is forced to `'0'`

### Specification

* Asynchronous reset: `sreg ← 0000_0000`
* On `rising_edge(clk)`: `sreg ← sreg(6..0) & feed`
* `q` always reflects `sreg`.

### Where to write

Edit only between `-- BEGIN_EDIT` and `-- END_EDIT`. The shift process,
the reset path and the output assignment are locked.

### Hint

A single concurrent assignment using `when … else` is enough.
