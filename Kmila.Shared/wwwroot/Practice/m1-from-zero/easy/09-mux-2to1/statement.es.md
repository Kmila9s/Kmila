## Multiplexor 2 a 1

Enruta `a` o `b` a `y` según `sel`. Si `sel = 0`, la salida es `a`; si `sel = 1`, es `b`.

| `sel` | `y`   |
|:-----:|:-----:|
|  0    |  `a`  |
|  1    |  `b`  |

### Pista

La asignación condicional `when ... else` funciona bien:
`y <= a when sel = '0' else b;`
