## Codificador de prioridad 4 a 2

Codifica el índice del bit activo más alto de `d` en la salida de 2 bits `y`. Los bits con índice mayor tienen prioridad.

| `d` | `y` |
|:---:|:---:|
| `1xxx` | 11 |
| `01xx` | 10 |
| `001x` | 01 |
| `0001` | 00 |
| `0000` | 00 |

### Pista

Una cadena de `when ... else` de mayor a menor prioridad da la semántica gratis.
