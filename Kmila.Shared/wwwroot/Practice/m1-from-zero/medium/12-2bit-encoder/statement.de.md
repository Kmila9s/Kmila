## 4-zu-2-Prioritätscoder

Kodiere den Index des höchsten aktiven Bits von `d` in den 2-Bit-Ausgang `y`. Höher indexierte Bits haben Vorrang.

| `d` | `y` |
|:---:|:---:|
| `1xxx` | 11 |
| `01xx` | 10 |
| `001x` | 01 |
| `0001` | 00 |
| `0000` | 00 |

### Hinweis

Eine `when ... else`-Kette von höchster zu niedrigster Priorität liefert die Semantik direkt.
