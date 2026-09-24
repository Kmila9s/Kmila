## Demi-additionneur — sorties inversées

Ce demi-additionneur a ses deux affectations de sortie
**inversées** : l'expression qui devrait piloter `sum` est sur
`cout`, et inversement.

Trouve le bug et remets chaque sortie sur la bonne ligne.

### Spécification

* `sum  = a ⊕ b`
* `cout = a ∧ b`

### Indice

XOR donne le bit de somme ; AND donne le bit de retenue.
