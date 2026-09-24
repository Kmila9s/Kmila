## 4-Bit Even Parity — fülle die Lücke

Das Even-Parity-Bit `p` eines 4-Bit-Wortes `d` ist `1`, wenn die
Anzahl der `1`-Bits in `d` **ungerade** ist, sonst `0` — also genau
das XOR der vier Eingangsbits.

### Verhalten

`p <= d(0) xor d(1) xor d(2) xor d(3)`

### Beispiele

| `d`   | Einsen | `p` |
|:------|:------:|:---:|
| 0000  |   0    |  0  |
| 0001  |   1    |  1  |
| 0011  |   2    |  0  |
| 1110  |   3    |  1  |
| 1111  |   4    |  0  |

### Wo schreiben

Entity, Ports und Gerüst sind vorhanden. Nur die Zeile zwischen
`-- BEGIN_EDIT` und `-- END_EDIT` wird bewertet — alles andere ist
gesperrt.
