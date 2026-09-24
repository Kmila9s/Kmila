## MUX 2 vers 1 — trouve et corrige le bug

Un multiplexeur 2 vers 1 doit router `a` vers `y` quand `sel = 0`,
et `b` vers `y` quand `sel = 1`. L'auteur a **inversé** la
polarité de `sel`.

Trouve le bug et corrige-le.

### Spécification

* `sel = 0` → `y = a`
* `sel = 1` → `y = b`

### Indice

Regarde bien la valeur de `sel` utilisée dans la clause `when`.
