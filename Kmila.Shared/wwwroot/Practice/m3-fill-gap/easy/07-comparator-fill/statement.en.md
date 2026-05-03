## 4-bit equality comparator — fill the gap

Implement a 4-bit equality comparator. The output `eq` is `1` when
the two 4-bit vectors `a` and `b` are bit-exactly equal, otherwise
`0`.

### Behavior

* `eq <= '1'` when `a = b`
* `eq <= '0'` otherwise

### Hint

Use a `when ... else` concurrent assignment with the `=` operator
on the full vectors.

### Where to write

The entity, ports and architecture skeleton are already in place.
Only the line between `-- BEGIN_EDIT` and `-- END_EDIT` is graded —
everything outside is locked scaffolding.
