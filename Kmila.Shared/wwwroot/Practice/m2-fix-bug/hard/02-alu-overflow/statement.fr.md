## ALU — corrigez le flag de débordement signé

L'ALU 8 bits calcule `a + b` en complément à deux et active `ovf`
quand le résultat signé ne tient pas sur 8 bits. L'implémentation
actuelle est "presque correcte" — elle reprend la forme de la
formule du cours mais lit le mauvais bit.

### Spécification

* Entrées `a`, `b` : entiers signés 8 bits (complément à deux).
* Soit `sum = a + b` (tronqué à 8 bits).
* Le débordement signé survient **si et seulement si** les deux
  opérandes ont le même signe et le résultat a le signe opposé.
* Formellement : `ovf = (a[7] = b[7]) AND (sum[7] /= a[7])`.

### Indice

Le bit 7 est le bit de signe en complément à deux 8 bits. Lire un
autre bit rend la formule mathématiquement absurde.
