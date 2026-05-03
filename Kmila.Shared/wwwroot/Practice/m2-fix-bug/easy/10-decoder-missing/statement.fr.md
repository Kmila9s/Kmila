## Décodeur 2 vers 4 — trouve et corrige le bug

Un décodeur 2 vers 4 active exactement **un** des quatre bits de
sortie selon `sel`. Trois des quatre cas sont gérés
correctement, mais un cas a été oublié et tombe sur la valeur
par défaut `"0000"`.

Trouve le cas manquant et ajoute-le.

### Spécification

| `sel` | `y`     |
|:-----:|:-------:|
| 00    | 0001    |
| 01    | 0010    |
| 10    | 0100    |
| 11    | 1000    |

### Indice

Regarde quelle valeur de `sel` n'apparaît pas dans les `when`.
