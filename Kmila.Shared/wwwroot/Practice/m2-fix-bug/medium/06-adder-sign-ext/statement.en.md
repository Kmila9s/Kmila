## Mixed-width signed adder — sign extension bug

`a` is a 4-bit two's-complement number (range −8…+7) and `b` is an 8-bit two's-complement number. The output `sum = a + b` is 8 bits, also two's-complement.

### Specification

* Sign-extend `a` from 4 bits to 8 bits, then add to `b`.
* Replicate the sign bit `a(3)` into the upper four bits of the extended value.

### What's wrong

The author extended `a` by simply prepending `"0000"`, which is **zero-extension**. For positive `a` it happens to work, but for negative `a` (e.g. `1111` = −1) it produces a positive number 15 instead of −1, and the sum is wrong.

### Hint

`numeric_std` provides `resize(signed_value, new_width)`, which performs sign-extension automatically when the source is `signed`.
