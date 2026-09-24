## Parité 4 bits — trouve et corrige le bug

Ce circuit doit produire la **parité** du vecteur 4 bits `d` :
`p = 1` si un nombre impair de bits de `d` valent `1`, `p = 0`
sinon. L'auteur a tenté un chaînage de portes NON-ET — sans
succès.

Trouve le bug et utilise le bon opérateur.

### Spécification

* `p = d(0) ⊕ d(1) ⊕ d(2) ⊕ d(3)`

### Indice

La parité, c'est exactement ce que calcule le XOR — pas un
chaînage de NAND.
