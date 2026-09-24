## Multiplieur 4×4 — corrigez l'extension de signe asymétrique

Deux valeurs non signées 4 bits `a` et `b` sont multipliées pour
former un `product` non signé 8 bits. Le code actuel interprète par
erreur `a` comme **signé** tout en gardant `b` non signé, ce qui
donne un mauvais résultat dès que `a(3) = '1'` (i.e. `a >= 8`).

### Spécification

* `a`, `b` sont des entiers **non signés** 4 bits dans `0..15`.
* `product = a * b` interprété en **non signé** 8 bits
  (plage `0..225`).

### Indice

Castez les deux opérandes de la même façon :
`unsigned(a) * unsigned(b)`. Le résultat tient déjà exactement
sur 8 bits — pas besoin de `resize`.
