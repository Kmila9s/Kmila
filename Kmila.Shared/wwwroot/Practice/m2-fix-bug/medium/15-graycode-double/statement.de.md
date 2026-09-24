## 3-Bit Binär→Gray — Gray-Eigenschaft verletzt

Ein Standard-Binär→Gray-Konverter. Die klassische Vorschrift für `g`, gegeben das Binär `b`:

* `g(2) = b(2)`
* `g(1) = b(2) xor b(1)`
* `g(0) = b(1) xor b(0)`

Sie garantiert, dass benachbarte Binärwerte (`b` und `b+1`) sich in genau einem Gray-Bit unterscheiden.

| `b`  | Gray |
|:----:|:----:|
| 000  | 000  |
| 001  | 001  |
| 010  | 011  |
| 011  | 010  |
| 100  | 110  |

### Was schiefläuft

Der Starter berechnet `g(2)` falsch und mischt `b(0)` hinein. Beim Übergang von `b = 000` nach `b = 001` kippen `g(0)` **und** `g(2)` gleichzeitig — zwei Bits ändern sich, genau das, was Gray-Code vermeiden soll.

### Hinweis

`g(2)` darf nur vom **MSB** der Binäreingabe abhängen.
