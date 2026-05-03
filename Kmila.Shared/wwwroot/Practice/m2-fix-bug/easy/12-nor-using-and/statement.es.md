## Compuerta NOR — encuentra y corrige el error

Se pidió al autor implementar una compuerta NOR de 2 entradas.
La expresión `a nor b` estaba casi bien, pero a uno de los
operandos se le aplicó un `not` de más, que rompe la tabla de
verdad.

Encuentra la inversión sobrante y elimínala.

### Especificación

| `a` | `b` | `y` |
|:---:|:---:|:---:|
|  0  |  0  |  1  |
|  0  |  1  |  0  |
|  1  |  0  |  0  |
|  1  |  1  |  0  |

### Pista

Las dos entradas de un NOR entran directamente — sin inversor
extra.
