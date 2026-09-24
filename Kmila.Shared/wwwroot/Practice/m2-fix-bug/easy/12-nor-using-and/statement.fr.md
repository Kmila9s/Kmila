## Porte NON-OU — trouve et corrige le bug

L'auteur devait implémenter une porte NON-OU à 2 entrées.
L'expression `a nor b` y était presque, mais un `not` superflu a
été appliqué à un des opérandes, ce qui casse la table de
vérité.

Trouve l'inversion en trop et supprime-la.

### Spécification

| `a` | `b` | `y` |
|:---:|:---:|:---:|
|  0  |  0  |  1  |
|  0  |  1  |  0  |
|  1  |  0  |  0  |
|  1  |  1  |  0  |

### Indice

Les deux entrées d'un NON-OU arrivent directement — pas
d'inverseur supplémentaire.
