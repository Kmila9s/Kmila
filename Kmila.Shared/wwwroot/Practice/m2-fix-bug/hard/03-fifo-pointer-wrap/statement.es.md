## FIFO — corrige el envoltorio del puntero

Una FIFO de profundidad 4 pierde datos cuando el buffer da la vuelta.
La lógica de `count`/`full`/`empty` parece correcta, pero los
punteros de lectura y escritura se envuelven una posición demasiado
tarde, así que la FIFO se comporta como si tuviera cinco entradas
cuando solo tiene cuatro.

### Especificación

* FIFO síncrona de profundidad 4 con reset activo-alto.
* Los punteros toman valores `0..3` y **deben volver a 0 tras el 3**.
* `full = '1'` cuando `count = 4`, `empty = '1'` cuando `count = 0`.
* Los push se ignoran si está llena; los pop si está vacía.

### Pista

Si `DEPTH = 4`, el rango legal del puntero es `0..DEPTH-1`. Envolver
al comparar con `DEPTH` (o peor, con `DEPTH+1`) introduce un hueco
fantasma.
