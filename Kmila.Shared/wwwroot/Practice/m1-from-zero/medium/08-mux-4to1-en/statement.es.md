## Mux 4 a 1 con habilitación

Implementa un multiplexor 4 a 1 con habilitación activa en alto. Cuando `en = '0'`, la salida es `0`. Cuando `en = '1'`, la salida `y` sigue una de las entradas:

| `sel` | `y` |
|:---:|:---:|
| 00 | a |
| 01 | b |
| 10 | c |
| 11 | d |

### Pista

Una sola asignación concurrente condicional con cadena `when ... else` resuelve el caso.
