## MUX 2 a 1 — rama por defecto incorrecta

Un MUX 2 a 1 debe entregar `a` cuando `sel = 0` y `b` cuando
`sel = 1`. El starter enruta `a` bien con `sel = 0`, pero con
`sel = 1` pone `'1'` en lugar de `b` — `b` nunca se lee.

Encuentra el error y usa `b` en la rama `else`.

### Especificación

* `sel = 0` → `y = a`
* `sel = 1` → `y = b`

### Pista

Mira qué literal hay después del `else` — debería ser una
**señal**, no una constante.
