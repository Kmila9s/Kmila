## Additionneur complet — trouver et corriger le bug

L'auteur a essayé d'implémenter un additionneur 1-bit complet. La sortie `sum` est correcte, mais la sortie `cout` est **fausse** pour au moins une combinaison.

Trouve le bug dans l'architecture et corrige-le.

### Spécification

Pour `a`, `b`, `cin` ∈ {0, 1} :

* `sum  = a ⊕ b ⊕ cin`
* `cout = (a ∧ b) ∨ (cin ∧ (a ⊕ b))`

### Indice

La retenue de sortie doit prendre `cin` en compte — pas seulement `a` et `b`.
