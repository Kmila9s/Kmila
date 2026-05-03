## Sumador medio — completa el acarreo de salida

Un sumador medio toma dos entradas de 1 bit `a` y `b` y produce un
resultado de 2 bits: un bit de suma y un bit de acarreo.

La línea de la **suma** `sum <= a xor b;` ya está fuera de la región
editable. Tu tarea es escribir la asignación del **acarreo de
salida**.

### Comportamiento

* `sum  <= a xor b`  *(dado)*
* `cout <= a and b`  *(a escribir)*

### Tabla de verdad

| `a` | `b` | `sum` | `cout` |
|:---:|:---:|:-----:|:------:|
|  0  |  0  |   0   |   0    |
|  0  |  1  |   1   |   0    |
|  1  |  0  |   1   |   0    |
|  1  |  1  |   0   |   1    |

### Dónde escribir

Sólo la línea entre `-- BEGIN_EDIT` y `-- END_EDIT` se evalúa.
La asignación de `sum` fuera de los marcadores no debe tocarse.
