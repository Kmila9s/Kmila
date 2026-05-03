## Demi-soustracteur

Calcule `a − b` sur 1 bit.

| `a` | `b` | `diff` | `borrow` |
|:---:|:---:|:------:|:--------:|
|  0  |  0  |   0    |    0     |
|  0  |  1  |   1    |    1     |
|  1  |  0  |   1    |    0     |
|  1  |  1  |   0    |    0     |

### Indice

`diff = a xor b`, `borrow = (not a) and b`.
