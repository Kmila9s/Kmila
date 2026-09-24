## Encodeur Manchester — demi-bits inversés

L'encodeur doit suivre la convention G.E. Thomas (IEEE 802.3) :
- `din = '0'` → `m = '01'`
- `din = '1'` → `m = '10'`

Cela se résume à `m = din xor phase`.

Le starter inverse l'encodage (renvoie `'10'` pour `0` et `'01'` pour `1`). Trouve et corrige l'opérateur fautif.
