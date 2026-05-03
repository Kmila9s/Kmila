## Half-subtractor

Compute `a − b` on a single bit.

| `a` | `b` | `diff` | `borrow` |
|:---:|:---:|:------:|:--------:|
|  0  |  0  |   0    |    0     |
|  0  |  1  |   1    |    1     |
|  1  |  0  |   1    |    0     |
|  1  |  1  |   0    |    0     |

### Hint

`diff = a xor b`, `borrow = (not a) and b`.
