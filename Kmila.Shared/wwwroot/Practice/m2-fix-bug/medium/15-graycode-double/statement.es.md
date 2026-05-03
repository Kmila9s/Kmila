## Conversor binario→Gray de 3 bits — propiedad de Gray rota

Se pedía un conversor estándar binario→Gray. La receta clásica para `g`, dado el binario `b`, es:

* `g(2) = b(2)`
* `g(1) = b(2) xor b(1)`
* `g(0) = b(1) xor b(0)`

Esto garantiza que valores binarios adyacentes (`b` y `b+1`) difieren en exactamente un bit de Gray.

| `b`  | Gray |
|:----:|:----:|
| 000  | 000  |
| 001  | 001  |
| 010  | 011  |
| 011  | 010  |
| 100  | 110  |

### Qué falla

El starter calcula mal `g(2)` mezclando `b(0)`. Al pasar de `b = 000` a `b = 001` cambian a la vez `g(0)` **y** `g(2)`: dos bits cambian, justo lo que Gray pretende evitar.

### Pista

`g(2)` debe depender únicamente del **MSB** de la entrada binaria.
