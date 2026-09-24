## FIFO drapeaux vide/plein

Implémente le compteur d'occupation d'une FIFO de profondeur 4. Tu n'exposes que `full` et `empty` — la voie de données n'est pas modélisée.

### Spécification

- Compteur `unsigned` 3 bits `cnt` dans 0..4.
- À chaque cycle :
  - si seul `push='1'` et `cnt < 4`, `cnt <= cnt + 1`.
  - si seul `pop='1'` et `cnt > 0`, `cnt <= cnt - 1`.
  - sinon, conserve la valeur.
- `empty <= '1'` quand `cnt = 0`.
- `full <= '1'` quand `cnt = 4`.

### Indice

Utilise `to_unsigned(4, 3)` pour comparer et borne aux limites.
