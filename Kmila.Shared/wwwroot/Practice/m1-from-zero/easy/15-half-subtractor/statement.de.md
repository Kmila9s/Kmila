## Halbsubtrahierer

Berechne `a − b` auf 1 Bit.

| `a` | `b` | `diff` | `borrow` |
|:---:|:---:|:------:|:--------:|
|  0  |  0  |   0    |    0     |
|  0  |  1  |   1    |    1     |
|  1  |  0  |   1    |    0     |
|  1  |  1  |   0    |    0     |

### Hinweis

`diff = a xor b`, `borrow = (not a) and b`.
