## Porte XOR — remplis le trou

L'entité, les ports et le squelette de l'architecture sont déjà en
place. Implémente la **seule affectation concurrente** qui pilote
`y` avec le OU exclusif de `a` et `b` (vrai quand les entrées
diffèrent).

### Table de vérité

| `a` | `b` | `y` |
|:---:|:---:|:---:|
|  0  |  0  |  0  |
|  0  |  1  |  1  |
|  1  |  0  |  1  |
|  1  |  1  |  0  |

### Où écrire

Seule la ligne entre `-- BEGIN_EDIT` et `-- END_EDIT` est évaluée.
Tout le reste est verrouillé.

### Indice

Un opérateur : `xor`.
