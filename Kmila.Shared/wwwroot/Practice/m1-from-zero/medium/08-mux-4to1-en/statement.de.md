## 4-zu-1-Mux mit Enable

Implementiere einen 4-zu-1-Multiplexer mit aktiv-hohem Enable. Bei `en = '0'` ist die Ausgabe `0`. Bei `en = '1'` folgt `y` einem der Eingänge:

| `sel` | `y` |
|:---:|:---:|
| 00 | a |
| 01 | b |
| 10 | c |
| 11 | d |

### Hinweis

Eine einzige konkurrente bedingte Zuweisung mit `when ... else`-Kette löst die Aufgabe.
