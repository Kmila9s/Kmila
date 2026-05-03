## Paridad par de 4 bits — completa el hueco

El bit de paridad par `p` de una palabra de 4 bits `d` vale `1`
cuando la cantidad de bits a `1` en `d` es **impar**, y `0` cuando
es par — es exactamente la XOR de los cuatro bits.

### Comportamiento

`p <= d(0) xor d(1) xor d(2) xor d(3)`

### Ejemplos

| `d`   | unos | `p` |
|:------|:----:|:---:|
| 0000  |  0   |  0  |
| 0001  |  1   |  1  |
| 0011  |  2   |  0  |
| 1110  |  3   |  1  |
| 1111  |  4   |  0  |

### Dónde escribir

La entidad, los puertos y la estructura ya están listos. Sólo la
línea entre `-- BEGIN_EDIT` y `-- END_EDIT` se evalúa — todo lo
demás es estructura bloqueada.
