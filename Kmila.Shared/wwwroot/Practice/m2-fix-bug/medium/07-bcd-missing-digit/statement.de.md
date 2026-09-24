## BCD-zu-7-Segment-Dekoder — eine Ziffer ist dunkel

Ein Standard-BCD-zu-7-Segment-Dekoder (Segmente high-aktiv, Reihenfolge `a b c d e f g`) für die Ziffern 0…9.

### Segmentmuster

| Ziffer | seg (`abcdefg`) |
|:------:|:---------------:|
|   0    | `1111110` |
|   1    | `0110000` |
|   7    | `1110000` |
|   8    | `1111111` |
|   9    | `1111011` |

### Was schiefläuft

Die meisten Ziffern werden korrekt dekodiert, aber **Ziffer 7** liefert `"0000000"` (Anzeige aus) statt der erwarteten `"1110000"` (Segmente `a`, `b`, `c` an).

### Hinweis

Sieh dir die Zeile `when "0111"` des Selected Assignment an.
