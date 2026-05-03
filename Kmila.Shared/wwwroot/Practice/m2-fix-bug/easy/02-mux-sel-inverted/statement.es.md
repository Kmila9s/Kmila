## MUX 2 a 1 — encuentra y corrige el error

Un multiplexor 2 a 1 debe enrutar `a` a `y` cuando `sel = 0`, y
`b` a `y` cuando `sel = 1`. El autor invirtió la polaridad de
`sel`.

Encuentra el error y corrígelo.

### Especificación

* `sel = 0` → `y = a`
* `sel = 1` → `y = b`

### Pista

Mira con atención el valor de `sel` usado en la cláusula `when`.
