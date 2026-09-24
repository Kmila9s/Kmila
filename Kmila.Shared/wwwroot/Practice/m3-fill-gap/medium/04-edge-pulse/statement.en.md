## Rising-edge pulse — fill the gap

A two-stage sampler is wired up: every clock cycle, the previous and
current samples of `d` are stored as `d_prev` and `d_curr`. The
register reset is also already in place.

You must produce `pulse`: it should be `'1'` for **exactly one clock**
cycle whenever `d` transitions from low to high, and `'0'` otherwise.

### Specification

* `pulse` is a combinational function of `d_curr` and `d_prev`.
* `pulse = '1'` iff `d_curr = '1'` and `d_prev = '0'`.

### Where to write

Edit only between `-- BEGIN_EDIT` and `-- END_EDIT`. The sampling
process and the reset are locked.

### Hint

`pulse <= d_curr and (not d_prev);`
