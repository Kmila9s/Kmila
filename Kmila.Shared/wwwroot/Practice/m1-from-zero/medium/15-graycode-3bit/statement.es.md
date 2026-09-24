## Código Gray de 3 bits

Convierte una entrada binaria de 3 bits `b` en código Gray `g`. En Gray, sólo un bit cambia entre valores consecutivos.

### Ecuaciones

- `g(2) = b(2)`
- `g(1) = b(2) XOR b(1)`
- `g(0) = b(1) XOR b(0)`

### Tabla de verdad

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
