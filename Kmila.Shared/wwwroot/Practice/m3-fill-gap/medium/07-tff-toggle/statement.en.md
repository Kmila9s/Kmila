## T flip-flop — fill the toggle line

A T flip-flop is **almost** complete: the reset path, the rising-edge
detection and the output `q <= qi` are already wired up.

You must implement the toggle behaviour inside the rising-edge branch:

* if `t = '1'` → `qi` flips its value
* if `t = '0'` → `qi` stays the same

### Specification

* Asynchronous reset: `qi ← '0'`.
* On `rising_edge(clk)`: `qi ← qi xor t`.

### Where to write

Edit only between `-- BEGIN_EDIT` and `-- END_EDIT`. The reset path
and the output assignment are locked.

### Hint

`qi <= qi xor t;`
