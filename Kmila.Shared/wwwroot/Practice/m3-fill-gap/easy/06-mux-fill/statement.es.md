## Multiplexor 2 a 1 — completa el hueco

Un multiplexor 2 a 1 dirige una de las dos entradas a la salida
según el bit selector `sel`.

* Cuando `sel = '0'`, `y` sigue a `a`.
* Cuando `sel = '1'`, `y` sigue a `b`.

La entidad, los puertos y la estructura de la arquitectura ya están
listos. Implementa la **única asignación concurrente** con una
expresión `when ... else`.

### Tabla de verdad

| `sel` | `a` | `b` | `y` |
|:-----:|:---:|:---:|:---:|
|   0   |  0  |  X  |  0  |
|   0   |  1  |  X  |  1  |
|   1   |  X  |  0  |  0  |
|   1   |  X  |  1  |  1  |

### Dónde escribir

Sólo la línea entre `-- BEGIN_EDIT` y `-- END_EDIT` se evalúa.
Todo lo demás es estructura bloqueada.
