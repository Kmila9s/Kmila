## 3-Bit-Gray-Code

Wandle einen 3-Bit-Binäreingang `b` in den Gray-Code `g` um. Im Gray-Code ändert sich zwischen aufeinanderfolgenden Werten genau ein Bit.

### Gleichungen

- `g(2) = b(2)`
- `g(1) = b(2) XOR b(1)`
- `g(0) = b(1) XOR b(0)`

### Wahrheitstabelle

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
