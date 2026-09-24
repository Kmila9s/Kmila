## 3-bit Gray code

Convert a 3-bit binary input `b` into Gray code `g`. In Gray code, only one bit changes between consecutive values.

### Equations

- `g(2) = b(2)`
- `g(1) = b(2) XOR b(1)`
- `g(0) = b(1) XOR b(0)`

### Truth table

| `b` | `g` |
|:---:|:---:|
| 000 | 000 |
| 001 | 001 |
| 010 | 011 |
| 011 | 010 |
| 100 | 110 |
| 101 | 111 |
| 110 | 101 |
| 111 | 100 |
