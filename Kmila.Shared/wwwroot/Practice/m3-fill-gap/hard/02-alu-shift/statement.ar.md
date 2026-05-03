## ⚠ Translation pending — ALU shift-left — fill the gap

A barrel shifter is **almost** done. The entity already declares
the 8-bit data input `a`, the 3-bit shift amount `sh`, and the
8-bit output `y`. The grader has wired up the surrounding
`process(a, sh)`.

Your job is to assign `y` to `a` shifted **left** by `sh`
positions, treating zero-fill on the right.

### Hint

```vhdl
y <= std_logic_vector(shift_left(unsigned(a), to_integer(unsigned(sh))));
```

### Where to write

Only the lines between `-- BEGIN_EDIT` and `-- END_EDIT` are
graded.
