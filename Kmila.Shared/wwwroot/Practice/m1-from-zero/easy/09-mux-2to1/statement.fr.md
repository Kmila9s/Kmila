## Multiplexeur 2 vers 1

Route `a` ou `b` vers `y` selon `sel`. Si `sel = 0`, la sortie vaut `a`; si `sel = 1`, elle vaut `b`.

| `sel` | `y`   |
|:-----:|:-----:|
|  0    |  `a`  |
|  1    |  `b`  |

### Indice

L'affectation conditionnelle `when ... else` est concise :
`y <= a when sel = '0' else b;`
