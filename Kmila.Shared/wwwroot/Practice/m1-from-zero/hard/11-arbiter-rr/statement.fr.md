## Arbitre round-robin (4)

Implémente un arbitre round-robin 4 canaux. À chaque coup, accorde exactement une requête — `grant` est one-hot (ou tout à zéro si aucune `req`).

### Spécification

- Garde `last`, index 2 bits du dernier vainqueur (0 après reset).
- À chaque front, parcours `req` à partir de `last + 1` (wrap-around 2 bits).
- Le premier bit à `'1'` gagne : positionne ce bit dans `grant` et mets à jour `last`.
- Si aucun bit n'est `'1'`, `grant` reste `0000` et `last` ne change pas.

### Indice

`for i in 0 to 3 loop` + variable de garde pour t'arrêter au premier match. `to_integer(idx)` indexe le vecteur.
