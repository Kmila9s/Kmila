## Decodificador 2 a 4 — encuentra y corrige el error

Un decodificador 2 a 4 activa exactamente **una** de las cuatro
salidas según `sel`. Tres de los cuatro casos se manejan
correctamente, pero uno se olvidó y termina cayendo al valor por
defecto `"0000"`.

Encuentra el caso faltante y agrégalo.

### Especificación

| `sel` | `y`     |
|:-----:|:-------:|
| 00    | 0001    |
| 01    | 0010    |
| 10    | 0100    |
| 11    | 1000    |

### Pista

Revisa qué valor de `sel` no aparece en las cláusulas `when`.
