## Binary-to-Gray converter — fill the conversion

A 3-bit binary counter `b(2..0)` is already running and reset-cleared.
The output bus `g` is driven from a 3-bit Gray-code signal `gi`. Your
job is to compute `gi` from `b`.

### Specification

The standard binary-to-Gray conversion is:

```
gi(2) = b(2)
gi(1) = b(2) xor b(1)
gi(0) = b(1) xor b(0)
```

It produces the sequence `000, 001, 011, 010, 110, 111, 101, 100, …`
where each step changes exactly one bit.

### Where to write

Edit only between `-- BEGIN_EDIT` and `-- END_EDIT`. The counter, the
reset and the output assignment `g <= gi` are locked.

### Hint

Three concurrent assignments on the bits of `gi`, or one combined
assignment using `b(2) & (b(2) xor b(1)) & (b(1) xor b(0))`.
