## One-hot vers binaire

Convertis une entrée one-hot 4 bits `oh` en son index binaire 2 bits `b`. Exactement un bit de `oh` vaut `1`.

| `oh` | `b` |
|:---:|:---:|
| 0001 | 00 |
| 0010 | 01 |
| 0100 | 10 |
| 1000 | 11 |

### Indice

Une cascade `when ... else` suffit. La sortie est la position du seul `1`.
