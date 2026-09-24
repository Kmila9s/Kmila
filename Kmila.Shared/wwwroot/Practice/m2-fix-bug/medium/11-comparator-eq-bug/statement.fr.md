## Comparateur 4 bits — sortie d'égalité bloquée

Un comparateur non signé 4 bits avec trois drapeaux :

* `gt = '1'` quand `a > b`.
* `eq = '1'` quand `a = b`.
* `lt = '1'` quand `a < b`.

### Ce qui cloche

`gt` et `lt` sont corrects, mais `eq` est figé à `'0'`. Avec des entrées égales (p. ex. `a = b = 0101`), les **trois** drapeaux sont bas — un état incohérent pour un comparateur.

### Indice

Remplace l'affectation constante de `eq` par une expression conditionnelle similaire à celles de `gt` et `lt`, en testant l'égalité.
