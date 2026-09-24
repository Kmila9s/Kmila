## ET à 3 entrées — trouve et corrige le bug

L'auteur devait écrire une porte ET à 3 entrées, mais le code
actuel ne fait le ET que sur `a` et `b`. La troisième entrée
`c` est ignorée.

Trouve le bug et remets `c` dans l'expression.

### Spécification

* `y = a ∧ b ∧ c`
* `y = 1` uniquement quand les trois entrées valent `1`.

### Indice

Il manque un opérande à droite du `<=`.
