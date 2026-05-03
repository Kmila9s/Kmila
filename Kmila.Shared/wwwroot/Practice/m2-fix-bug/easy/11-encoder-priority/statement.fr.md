## Encodeur prioritaire 4 vers 2 — trouve et corrige le bug

Un encodeur prioritaire 4 vers 2 sort l'**indice du bit le plus
haut à `1`** dans `d`. L'auteur a écrit une chaîne de clauses
`when` qui parcourt les bits dans le **mauvais ordre**, si bien
que lorsque plusieurs bits valent `1`, l'encodeur renvoie
l'indice le plus bas au lieu du plus haut.

Trouve le bug et corrige l'ordre de priorité.

### Spécification

* Si `d(3) = 1` → `y = "11"`
* Sinon si `d(2) = 1` → `y = "10"`
* Sinon si `d(1) = 1` → `y = "01"`
* Sinon → `y = "00"`

### Indice

`when ... else` est évalué **du haut vers le bas** — la première
clause qui correspond gagne.
