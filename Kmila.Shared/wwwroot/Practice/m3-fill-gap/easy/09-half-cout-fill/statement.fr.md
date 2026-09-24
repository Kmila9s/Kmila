## Demi-additionneur — remplis la retenue sortante

Un demi-additionneur prend deux entrées 1 bit `a` et `b` et produit
un résultat 2 bits : un bit de somme et un bit de retenue sortante.

La ligne de **somme** `sum <= a xor b;` est déjà fournie hors de la
zone éditable. Ton rôle est d'écrire l'affectation de la **retenue
sortante**.

### Comportement

* `sum  <= a xor b`  *(donné)*
* `cout <= a and b`  *(à écrire)*

### Table de vérité

| `a` | `b` | `sum` | `cout` |
|:---:|:---:|:-----:|:------:|
|  0  |  0  |   0   |   0    |
|  0  |  1  |   1   |   0    |
|  1  |  0  |   1   |   0    |
|  1  |  1  |   0   |   1    |

### Où écrire

Seule la ligne entre `-- BEGIN_EDIT` et `-- END_EDIT` est évaluée.
L'affectation de `sum` en dehors des marqueurs ne doit pas être
modifiée.
