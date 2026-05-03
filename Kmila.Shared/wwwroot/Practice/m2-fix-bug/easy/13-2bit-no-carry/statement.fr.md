## Additionneur 2 bits — trouve et corrige le bug

Ce circuit doit additionner deux nombres 2 bits `a` et `b` plus
une retenue d'entrée `cin`, produisant `sum` (2 bits) et un
`cout` final. L'auteur a écrit chaque bit de sortie comme un
demi-additionneur indépendant et **a oublié de propager la
retenue** du bit 0 vers le bit 1.

Trouve le bug et connecte correctement les retenues.

### Spécification

* `{cout, sum} = a + b + cin` (résultat 3 bits non signé).

### Indice

Le bit 1 de la somme doit tenir compte d'une retenue éventuelle
générée par le bit 0.
