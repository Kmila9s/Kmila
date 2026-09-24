## Medio restador

Calcula `a − b` para un bit.

| `a` | `b` | `diff` | `borrow` |
|:---:|:---:|:------:|:--------:|
|  0  |  0  |   0    |    0     |
|  0  |  1  |   1    |    1     |
|  1  |  0  |   1    |    0     |
|  1  |  1  |   0    |    0     |

### Pista

`diff = a xor b`, `borrow = (not a) and b`.
