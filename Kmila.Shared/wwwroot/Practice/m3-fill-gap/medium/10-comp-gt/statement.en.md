## 4-bit greater-than comparator — fill the gap

You are given an entity with two 4-bit operands `a` and `b` and a
single output `gt`. The architecture is empty except for the editable
region.

You must drive `gt`:

* `gt = '1'` when `a > b` (unsigned)
* `gt = '0'` otherwise

### Specification

* Treat `a` and `b` as **unsigned** numbers.
* Pure combinational — no clock involved.

### Where to write

Edit only between `-- BEGIN_EDIT` and `-- END_EDIT`. The entity is
locked.

### Hint

`gt <= '1' when unsigned(a) > unsigned(b) else '0';`
