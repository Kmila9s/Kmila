## Égalité 4 bits — trouve et corrige le bug

Ce circuit doit produire `eq = 1` quand les vecteurs `a` et `b`
de 4 bits sont égaux, et `0` sinon. Il marche dans beaucoup de
cas, mais déclare « égaux » certains couples qui ne le sont
clairement pas.

Trouve le bug et corrige-le.

### Spécification

* `eq = 1` si `a = b`, sinon `0`.
* Les quatre bits doivent participer à la comparaison.

### Indice

Vérifie l'intervalle d'indices utilisé dans la comparaison.
