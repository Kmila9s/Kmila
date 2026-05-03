## Multiplexor 4 a 1 — rellena todo el bloque de selección

La entidad declara las cuatro entradas `a, b, c, d`, el selector de
2 bits `sel` y la salida `y`. La arquitectura está vacía salvo por
la región editable.

Debes implementar toda la selección de 4 vías en una sola asignación
concurrente a `y`:

| `sel` | `y` |
|:---:|:---:|
| `00` | `a` |
| `01` | `b` |
| `10` | `c` |
| `11` | `d` |

### Dónde escribir

Edita solo entre `-- BEGIN_EDIT` y `-- END_EDIT`. La entidad está
bloqueada.

### Pista

Encadena tres `when … else` y deja que el `else` final cubra
`sel = "11"`.
