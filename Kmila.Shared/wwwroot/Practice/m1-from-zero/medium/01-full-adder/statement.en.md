## Full adder

Implement a 1-bit full adder. It adds three bits — `a`, `b`, and a carry-in `cin` — and produces `sum` and a carry-out `cout`.

### Equations

- `sum  = a XOR b XOR cin`
- `cout = (a AND b) OR (cin AND (a XOR b))`

### Hint

The XOR of the three inputs gives the sum. The carry-out is generated when at least two inputs are `1`.
