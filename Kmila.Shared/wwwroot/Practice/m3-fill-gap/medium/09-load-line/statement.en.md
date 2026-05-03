## 4-bit loadable register — fill the load branch

A 4-bit register with synchronous load is partly written. The reset
path, the rising-edge detection and the output `q <= qi` are already
in place.

You must implement the **load branch** inside the rising-edge block:

* if `load = '1'` → `qi ← d`
* if `load = '0'` → `qi` keeps its previous value

### Specification

* Asynchronous reset: `qi ← 0000`.
* On `rising_edge(clk)`:
  * `load = '1'` captures `d`.
  * `load = '0'` holds.
* `q` always reflects `qi`.

### Where to write

Edit only between `-- BEGIN_EDIT` and `-- END_EDIT`. The reset path
and the output assignment are locked.

### Hint

A short `if load = '1' then qi <= d; end if;` is enough — VHDL keeps
`qi` unchanged when no assignment is made.
