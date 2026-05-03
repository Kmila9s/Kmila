## ALU shift-left — fill the gap

A barrel shifter is **almost** done. The entity already declares
the 8-bit data input `a`, the 3-bit shift amount `sh`, and the
8-bit output `y`. The grader has wired up the surrounding
`process(a, sh)`.

Your job is to assign `y` to `a` shifted **left** by `sh`
positions, treating zero-fill on the right.

### Specification

* `numeric_std` is already `use`d.
* Convert `a` to `unsigned`, the shift amount via
  `to_integer(unsigned(sh))`, and call `shift_left(...)`.
* Convert the result back with `std_logic_vector(...)`.
* Bits shifted past the MSB are discarded; the LSBs become `0`.

### Hint

A single line is enough:

```vhdl
y <= std_logic_vector(shift_left(unsigned(a), to_integer(unsigned(sh))));
```

### Where to write

Only the lines between `-- BEGIN_EDIT` and `-- END_EDIT` are
graded.
