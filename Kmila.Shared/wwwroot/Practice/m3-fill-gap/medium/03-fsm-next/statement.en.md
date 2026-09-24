## 3-state FSM next-state — fill the gap

A 3-state Moore-style FSM is partly written. The state register, reset
behaviour and the transitions out of `"00"` and `"10"` are already in
place.

You must complete the **next-state assignment for state `"01"`**:

* if `x = '1'` → `nxt ← "10"`
* if `x = '0'` → `nxt ← "00"`

### Specification

* States: `"00"`, `"01"`, `"10"` (encoded directly).
* Asynchronous reset puts the FSM in `"00"`.
* `state` outputs the current state.

### Where to write

Edit only between `-- BEGIN_EDIT` and `-- END_EDIT`. The other branches
of the `case` and the state register are locked.

### Hint

A short `if x = '1' then … else … end if;` is enough.
