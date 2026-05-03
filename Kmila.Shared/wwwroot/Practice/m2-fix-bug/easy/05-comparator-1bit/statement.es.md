## Igualdad de 4 bits — encuentra y corrige el error

Este circuito debe entregar `eq = 1` cuando los vectores `a` y
`b` de 4 bits son iguales, y `0` en otro caso. Funciona en
muchos casos, pero declara "iguales" a algunos pares que
claramente no lo son.

Encuentra el error y corrígelo.

### Especificación

* `eq = 1` cuando `a = b`, en otro caso `0`.
* Los cuatro bits deben participar en la comparación.

### Pista

Revisa el rango de índices usado en la comparación.
