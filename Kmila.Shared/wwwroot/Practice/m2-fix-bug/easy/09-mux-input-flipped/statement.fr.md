## MUX 2 vers 1 — entrées inversées

Un MUX 2 vers 1 doit choisir `a` quand `sel = 0` et `b` quand
`sel = 1`. Ici, les **entrées de données** sont inversées : `b`
est sélectionné pour `sel = 0` et `a` pour `sel = 1`.

Trouve le bug et remets chaque entrée sur la bonne branche.

### Spécification

* `sel = 0` → `y = a`
* `sel = 1` → `y = b`

### Indice

La polarité de `sel` est correcte — pas les opérandes.
