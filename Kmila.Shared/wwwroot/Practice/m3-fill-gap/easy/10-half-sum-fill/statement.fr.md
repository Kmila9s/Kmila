## Demi-additionneur — remplis la somme

Un demi-additionneur prend deux entrées 1 bit `a` et `b` et produit
un résultat 2 bits : un bit de somme et un bit de retenue.

La ligne de **retenue sortante** `cout <= a and b;` est déjà fournie
hors de la zone éditable. Ton rôle est d'écrire l'affectation de la
**somme**.

### Comportement

* `cout <= a and b`  *(donné)*
* `sum  <= a xor b`  *(à écrire)*

### Table de vérité

| `a` | `b` | `sum` | `cout` |
|:---:|:---:|:-----:|:------:|
|  0  |  0  |   0   |   0    |
|  0  |  1  |   1   |   0    |
|  1  |  0  |   1   |   0    |
|  1  |  1  |   0   |   1    |

### Où écrire

Seule la ligne entre `-- BEGIN_EDIT` et `-- END_EDIT` est évaluée.
L'affectation de `cout` en dehors des marqueurs ne doit pas être
modifiée.
