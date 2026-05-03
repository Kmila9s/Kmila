## Multiplexor 4 a 1 — selector subdecodificado

Se pedía un mux 4 a 1 con `sel` de 2 bits:

| `sel` | `y` |
|:-----:|:---:|
| `00`  | `a` |
| `01`  | `b` |
| `10`  | `c` |
| `11`  | `d` |

### Qué falla

El autor solo mira `sel(0)`, así que el `case` se reduce a un mux 2 a 1 que solo enruta `a` (cuando `sel(0) = '0'`) o `b` (cuando `sel(0) = '1'`). Las entradas `c` y `d` son **inalcanzables**.

### Pista

Usa el vector `sel` **completo** en el `case` y pon una rama por valor binario: `"00"`, `"01"`, `"10"`, `"11"`.
