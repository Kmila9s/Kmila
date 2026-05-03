## Clock divider — fill the toggle

A 4-bit clock divider is **almost** complete: the modulo-10 counter
(`TOP = 9`), the reset and the output `clk_out <= clk_reg` are already
wired up. When the counter reaches `TOP`, it resets to `0`.

You must implement what happens to `clk_reg` **at the same instant**
the counter overflows: it should toggle. Outside this overflow event,
`clk_reg` keeps its previous value.

### Specification

* Asynchronous reset: `count ← 0`, `clk_reg ← '0'`.
* On `rising_edge(clk)`:
  * if `count = TOP` → `count ← 0`, `clk_reg ← not clk_reg`
  * else → `count ← count + 1`

### Where to write

Edit only between `-- BEGIN_EDIT` and `-- END_EDIT`. The counter,
reset and output are locked.

### Hint

`clk_reg <= not clk_reg;`
