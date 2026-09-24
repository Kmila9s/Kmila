## Porte OR — remplis le trou

L'entité, les ports et le squelette de l'architecture sont déjà en
place. Implémente la **seule affectation concurrente** qui pilote
`y` avec le OU logique de `a` et `b`.

### Table de vérité

| `a` | `b` | `y` |
|:---:|:---:|:---:|
|  0  |  0  |  0  |
|  0  |  1  |  1  |
|  1  |  0  |  1  |
|  1  |  1  |  1  |

### Où écrire

Seule la ligne entre `-- BEGIN_EDIT` et `-- END_EDIT` est évaluée.
Tout le reste est verrouillé.

### Indice

Un opérateur : `or`.
