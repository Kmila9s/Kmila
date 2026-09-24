## ALU de 4 bits — operación ausente

Se pedía una ALU de 4 operaciones, seleccionadas por `op`:

| `op` | operación |
|:----:|-----------|
| `00` | `a + b`   |
| `01` | `a - b`   |
| `10` | `a and b` |
| `11` | `a xor b` |

### Qué falla

Para `op = "11"` la salida está fijada a `"0000"` en lugar de calcular `a xor b`. La suma (`00`), la resta (`01`) y el AND (`10`) funcionan bien.

### Pista

Mira la rama `when others` del `case`. El resultado esperado en esa rama es un `xor` bit a bit, no una constante.
