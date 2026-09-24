## Arbitre 4 voies — corrigez la famine

Quatre agents pilotent `req(3..0)`. L'arbitre doit délivrer un seul
grant par cycle, mais l'implémentation actuelle est un codeur à
priorité fixe : la requête active de plus faible index gagne
toujours. Quand `req(0)` est bloqué à 1, `req(3)` est en
**famine permanente**.

### Spécification

* Reset synchrone actif-haut.
* À chaque cycle, exactement un bit de `grant` est à `'1'` (ou
  `0000` si `req = 0000`).
* Quand plusieurs requêtes sont actives, l'arbitre doit tourner —
  chaque demandeur passe en au plus 4 cycles.

### Indice

Un arbitre round-robin mémorise le **dernier index servi** et
scanne en partant de `last_grant + 1` (mod 4). La première requête
active rencontrée dans ce balayage l'emporte.
