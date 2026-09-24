## Puerta NOR — completa el hueco

La entidad, los puertos y la estructura de la arquitectura ya están
listos. Implementa la **única asignación concurrente** que lleva a
`y` la NOR lógica de `a` y `b` (la OR invertida).

### Tabla de verdad

| `a` | `b` | `y` |
|:---:|:---:|:---:|
|  0  |  0  |  1  |
|  0  |  1  |  0  |
|  1  |  0  |  0  |
|  1  |  1  |  0  |

### Dónde escribir

Sólo la línea entre `-- BEGIN_EDIT` y `-- END_EDIT` se evalúa.
Todo lo demás es estructura bloqueada.

### Pista

Un operador: `nor`.
