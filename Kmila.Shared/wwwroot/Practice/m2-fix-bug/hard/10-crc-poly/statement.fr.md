## CRC-8 — corrigez le polynôme

Ce générateur série de CRC-8 injecte un bit de rétroaction par
XOR dans des positions du registre choisies par le polynôme. Le
code actuel câble le mauvais polynôme : `0x07 = x^2 + x + 1` au
lieu de `0x1D = x^4 + x^3 + x^2 + 1`. Les CRC calculés ne
correspondront pas au checksum attendu.

### Spécification

* Reset synchrone actif-haut ; `crc` repart de `00000000`.
* À chaque cycle : `fb = reg(7) xor din`.
* `reg(0) <= fb`, et pour le polynôme `0x1D` :
  * `reg(2) <= reg(1) xor fb`
  * `reg(3) <= reg(2) xor fb`
  * `reg(4) <= reg(3) xor fb`
  * les autres bits décalent sans modification.

### Indice

Les taps XOR suivent les coefficients du polynôme `0x1D` (binaire
`00011101`). La version buggée XOR aux positions 1 et 2
seulement, ce qui correspond à `0x07`.
