## 2-bit ripple adder — fill the carry chain

Two full-adders are partly written. The sum bits and the final
carry-out are already in place; only the **internal carry** `c1`
between bit-0 and bit-1 is missing.

You must drive `c1` with the carry-out of the bit-0 full adder:

```
c1 = (a0·b0) + cin·(a0 ⊕ b0)
```

### Specification

* Inputs: `a`, `b` (2 bits each) and `cin`.
* Outputs: `sum` (2 bits) and `cout`.
* Bit-0 sum and bit-1 sum/cout are computed elsewhere — this exercise
  only fixes the wiring of `c1`.

### Where to write

Edit only between `-- BEGIN_EDIT` and `-- END_EDIT`. The other
concurrent assignments are locked scaffolding.

### Hint

`c1 <= (a(0) and b(0)) or (cin and (a(0) xor b(0)));`
