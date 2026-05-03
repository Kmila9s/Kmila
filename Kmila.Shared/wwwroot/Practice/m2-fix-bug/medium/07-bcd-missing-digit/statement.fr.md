## Décodeur BCD vers 7 segments — un chiffre éteint

Un décodeur BCD vers 7 segments standard (segments actifs hauts, ordre `a b c d e f g`) pour les chiffres 0…9.

### Motifs de segments

| chiffre | seg (`abcdefg`) |
|:-------:|:---------------:|
|    0    | `1111110` |
|    1    | `0110000` |
|    7    | `1110000` |
|    8    | `1111111` |
|    9    | `1111011` |

### Ce qui cloche

Presque tous les chiffres se décodent correctement, mais **le chiffre 7** sort `"0000000"` (afficheur éteint) au lieu du `"1110000"` attendu (segments `a`, `b`, `c` allumés).

### Indice

Regarde la ligne `when "0111"` du selected assignment.
