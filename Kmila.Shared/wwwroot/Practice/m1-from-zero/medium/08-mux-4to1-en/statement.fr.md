## Mux 4 vers 1 avec validation

Implémente un multiplexeur 4 vers 1 avec une entrée de validation active haut. Quand `en = '0'`, la sortie est forcée à `0`. Quand `en = '1'`, `y` suit l'une des entrées :

| `sel` | `y` |
|:---:|:---:|
| 00 | a |
| 01 | b |
| 10 | c |
| 11 | d |

### Indice

Une seule affectation concurrente conditionnelle avec une chaîne `when ... else` suffit.
