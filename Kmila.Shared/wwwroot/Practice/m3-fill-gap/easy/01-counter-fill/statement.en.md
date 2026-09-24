## 4-bit synchronous counter — fill the gap

A 4-bit counter is **almost** done. The reset path, the clock
edge detection, and the output assignment are already in place.

Implement the **single line** that advances the counter on every
rising clock edge.

### Specification

* Asynchronous reset: `q ← 0000`
* On `rising_edge(clk)` (when not in reset): `q ← q + 1`
* Wraps from `1111` back to `0000`

### Where to write

Only the line between the `-- BEGIN_EDIT` and `-- END_EDIT`
comments is meant to be edited. Everything else is the entity
scaffolding the grader expects.
