## Half-adder — outputs swapped

This half-adder has its two output assignments **swapped**: the
expression that should drive `sum` is on `cout`, and vice versa.

Find the bug and put each output on the right line.

### Specification

* `sum  = a ⊕ b`
* `cout = a ∧ b`

### Hint

XOR produces the sum bit; AND produces the carry bit.
