## Demi-additionneur

Construis un **demi-additionneur** 1 bit. Pour deux entrées 1-bit `a` et `b`, produis la somme bit à bit et la retenue de sortie.

### Table de vérité

| `a` | `b` | `sum` | `cout` |
|:---:|:---:|:-----:|:------:|
|  0  |  0  |   0   |   0    |
|  0  |  1  |   1   |   0    |
|  1  |  0  |   1   |   0    |
|  1  |  1  |   0   |   1    |

### Indice

Une porte par sortie : XOR pour `sum`, ET pour `cout`.
