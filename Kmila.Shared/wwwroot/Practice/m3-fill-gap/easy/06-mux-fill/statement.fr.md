## Multiplexeur 2 vers 1 — remplis le trou

Un multiplexeur 2 vers 1 aiguille l'une des deux entrées vers la
sortie selon le bit de sélection `sel`.

* Si `sel = '0'`, `y` suit `a`.
* Si `sel = '1'`, `y` suit `b`.

L'entité, les ports et le squelette de l'architecture sont déjà en
place. Implémente la **seule affectation concurrente** avec une
expression `when ... else`.

### Table de vérité

| `sel` | `a` | `b` | `y` |
|:-----:|:---:|:---:|:---:|
|   0   |  0  |  X  |  0  |
|   0   |  1  |  X  |  1  |
|   1   |  X  |  0  |  0  |
|   1   |  X  |  1  |  1  |

### Où écrire

Seule la ligne entre `-- BEGIN_EDIT` et `-- END_EDIT` est évaluée.
Tout le reste est verrouillé.
