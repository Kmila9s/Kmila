## Multiplexeur 4 vers 1 — sélecteur sous-décodé

Un mux 4 vers 1 avec un `sel` de 2 bits :

| `sel` | `y` |
|:-----:|:---:|
| `00`  | `a` |
| `01`  | `b` |
| `10`  | `c` |
| `11`  | `d` |

### Ce qui cloche

L'auteur ne teste que `sel(0)`, donc le `case` se réduit à un mux 2 vers 1 qui ne route que `a` (quand `sel(0) = '0'`) ou `b` (quand `sel(0) = '1'`). Les entrées `c` et `d` sont **inatteignables**.

### Indice

Utilise le vecteur `sel` **entier** dans le `case` et fournis une branche par valeur binaire : `"00"`, `"01"`, `"10"`, `"11"`.
