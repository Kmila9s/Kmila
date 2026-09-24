## One-hot a binario

Convierte una entrada one-hot de 4 bits `oh` en su índice binario de 2 bits `b`. Exactamente un bit de `oh` está en `1`.

| `oh` | `b` |
|:---:|:---:|
| 0001 | 00 |
| 0010 | 01 |
| 0100 | 10 |
| 1000 | 11 |

### Pista

Una simple cascada de `when ... else`. La salida es la posición del único `1`.
