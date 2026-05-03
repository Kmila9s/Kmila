## Up/down counter — fill the direction line

A 4-bit synchronous counter is **almost** complete. The reset path,
the rising-edge detection and the output `q <= std_logic_vector(count)`
are already wired up.

You must implement the direction control on the rising edge:

* `dir = '1'` → `count ← count + 1`
* `dir = '0'` → `count ← count - 1`

### Specification

* Asynchronous reset: `count ← 0000`.
* Counter wraps freely (4-bit unsigned arithmetic).

### Where to write

Edit only between `-- BEGIN_EDIT` and `-- END_EDIT`. The reset path
and the output assignment are locked.

### Hint

A short `if dir = '1' then … else … end if;` is enough.
