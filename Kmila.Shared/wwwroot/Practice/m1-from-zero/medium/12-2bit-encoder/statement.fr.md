## Codeur de priorité 4 vers 2

Encode l'indice du bit actif le plus haut de `d` dans la sortie 2 bits `y`. Les bits d'indice plus élevé sont prioritaires.

| `d` | `y` |
|:---:|:---:|
| `1xxx` | 11 |
| `01xx` | 10 |
| `001x` | 01 |
| `0001` | 00 |
| `0000` | 00 |

### Indice

Une cascade `when ... else` de la priorité la plus haute à la plus basse donne directement la sémantique.
