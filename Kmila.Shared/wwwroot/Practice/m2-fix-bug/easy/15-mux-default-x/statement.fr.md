## MUX 2 vers 1 — mauvaise branche par défaut

Un MUX 2 vers 1 doit produire `a` quand `sel = 0` et `b` quand
`sel = 1`. Le code de départ envoie bien `a` pour `sel = 0`,
mais pour `sel = 1` il met `'1'` à la place de `b` — `b` n'est
jamais lu.

Trouve le bug et utilise `b` dans la branche `else`.

### Spécification

* `sel = 0` → `y = a`
* `sel = 1` → `y = b`

### Indice

Regarde le littéral après `else` — ça devrait être un **signal**,
pas une constante.
