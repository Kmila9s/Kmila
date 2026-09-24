## Maître SPI (shift)

Implémente un maître SPI minimal qui décale un octet MSB-first quand `start='1'`.

### Spécification

- Un compteur `phase` 4 bits cadence l'opération ; actif, il s'incrémente à chaque coup.
- `sclk = phase(0)` quand actif — donne 8 paires haut/bas.
- `mosi = shreg(7)` quand actif — c'est le MSB courant.
- Décale `shreg` vers la gauche quand `phase(0) = '1'`.
- Reset remet tout à zéro. Le moteur redevient inactif après 16 phases (les tests n'observent que la fenêtre initiale).

### Indice

Un drapeau `active` maintient les sorties à `'0'` quand inactive.
