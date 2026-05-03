## Comparador de 4 bits — salida de igualdad atascada

Se pedía un comparador sin signo de 4 bits con tres banderas:

* `gt = '1'` cuando `a > b`.
* `eq = '1'` cuando `a = b`.
* `lt = '1'` cuando `a < b`.

### Qué falla

`gt` y `lt` están bien, pero `eq` está fijado a `'0'`, así que ante entradas iguales (p.ej. `a = b = 0101`) las **tres** banderas quedan en bajo: un estado inconsistente para un comparador.

### Pista

Cambia la asignación constante de `eq` por una expresión condicional como las de `gt` y `lt`, pero comprobando igualdad en lugar de orden.
