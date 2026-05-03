## 4-Bit-ALU — fehlende Operation

Eine ALU mit 4 per `op` ausgewählten Operationen:

| `op` | Operation |
|:----:|-----------|
| `00` | `a + b`   |
| `01` | `a - b`   |
| `10` | `a and b` |
| `11` | `a xor b` |

### Was schiefläuft

Für `op = "11"` ist die Ausgabe auf `"0000"` festverdrahtet, statt `a xor b` zu berechnen. Addition (`00`), Subtraktion (`01`) und AND (`10`) sind korrekt.

### Hinweis

Sieh dir den `when others`-Zweig des `case` an. Der erwartete Wert ist ein bitweises `xor`, keine Konstante.
