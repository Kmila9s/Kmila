## MUX 2 a 1 — entradas intercambiadas

Un MUX 2 a 1 debe seleccionar `a` cuando `sel = 0` y `b` cuando
`sel = 1`. Aquí las **entradas de datos** están intercambiadas:
se selecciona `b` con `sel = 0` y `a` con `sel = 1`.

Encuentra el error y deja cada entrada en su rama correcta.

### Especificación

* `sel = 0` → `y = a`
* `sel = 1` → `y = b`

### Pista

La polaridad de `sel` está bien — los operandos no.
