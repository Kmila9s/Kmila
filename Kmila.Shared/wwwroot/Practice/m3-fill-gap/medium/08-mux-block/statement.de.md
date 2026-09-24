## 4-zu-1-Multiplexer — gesamten Auswahlblock füllen

Die Entity deklariert die vier Eingänge `a, b, c, d`, das 2-Bit-
Auswahlsignal `sel` und den Ausgang `y`. Die Architektur ist außer
dem editierbaren Bereich leer.

Implementiere die gesamte 4-Weg-Auswahl in einer nebenläufigen
Zuweisung an `y`:

| `sel` | `y` |
|:---:|:---:|
| `00` | `a` |
| `01` | `b` |
| `10` | `c` |
| `11` | `d` |

### Wo schreiben

Nur zwischen `-- BEGIN_EDIT` und `-- END_EDIT` editieren. Die Entity
ist gesperrt.

### Hinweis

Drei `when … else` verketten; der finale `else` deckt `sel = "11"` ab.
