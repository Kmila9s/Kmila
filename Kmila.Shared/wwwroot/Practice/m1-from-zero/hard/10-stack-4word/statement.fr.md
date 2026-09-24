## Pile 4 mots — drapeaux

Implémente le compteur d'occupation d'une pile LIFO 4 mots. Seuls `full` et `empty` sont exposés.

### Spécification

- Compteur `unsigned` 3 bits `cnt` dans 0..4.
- À chaque cycle :
  - si seul `push='1'` et `cnt < 4`, `cnt <= cnt + 1`.
  - si seul `pop='1'` et `cnt > 0`, `cnt <= cnt - 1`.
  - sinon, conserve.
- `empty <= '1'` quand `cnt = 0`.
- `full <= '1'` quand `cnt = 4`.

### Indice

Le contrôle est identique à une FIFO de profondeur 4 — seule l'ordonnancement des données change, non modélisé ici.
