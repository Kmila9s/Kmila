## Halbaddierer — fülle die Summe

Ein Halbaddierer nimmt zwei 1-Bit-Eingänge `a` und `b` und liefert
ein 2-Bit-Ergebnis: ein Summenbit und ein Carry-Out-Bit.

Die **Carry-Out**-Zeile `cout <= a and b;` ist bereits außerhalb
des editierbaren Bereichs vorgegeben. Deine Aufgabe ist die
Zuweisung für die **Summe**.

### Verhalten

* `cout <= a and b`  *(gegeben)*
* `sum  <= a xor b`  *(zu schreiben)*

### Wahrheitstabelle

| `a` | `b` | `sum` | `cout` |
|:---:|:---:|:-----:|:------:|
|  0  |  0  |   0   |   0    |
|  0  |  1  |   1   |   0    |
|  1  |  0  |   1   |   0    |
|  1  |  1  |   0   |   1    |

### Wo schreiben

Nur die Zeile zwischen `-- BEGIN_EDIT` und `-- END_EDIT` wird
bewertet. Die `cout`-Zuweisung außerhalb der Marker darf nicht
verändert werden.
