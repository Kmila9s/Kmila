## Égalité 4 bits avec XOR — trouve et corrige le bug

Ce circuit doit produire `eq = 1` quand `a = b` et `0` sinon.
L'auteur a tenté de le calculer avec une longue chaîne de `xor`
sur les bits — mais cela donne la **parité**, pas l'égalité.

Trouve le bug et utilise la bonne comparaison.

### Spécification

* `eq = 1` quand `a = b`, sinon `0`.

### Indice

L'égalité est vraie quand **chaque paire** de bits correspond —
pas quand le nombre de `1` est pair ou impair.
