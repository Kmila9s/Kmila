## Halbaddierer — fülle den Carry-Out

Ein Halbaddierer nimmt zwei 1-Bit-Eingänge `a` und `b` und liefert
ein 2-Bit-Ergebnis: ein Summenbit und ein Carry-Out-Bit.

Die **Summen**-Zeile `sum <= a xor b;` ist bereits außerhalb des
editierbaren Bereichs vorgegeben. Deine Aufgabe ist die Zuweisung
für den **Carry-Out**.

### Verhalten

* `sum  <= a xor b`  *(gegeben)*
* `cout <= a and b`  *(zu schreiben)*

### Wahrheitstabelle

| `a` | `b` | `sum` | `cout` |
|:---:|:---:|:-----:|:------:|
|  0  |  0  |   0   |   0    |
|  0  |  1  |   1   |   0    |
|  1  |  0  |   1   |   0    |
|  1  |  1  |   0   |   1    |

### Wo schreiben

Nur die Zeile zwischen `-- BEGIN_EDIT` und `-- END_EDIT` wird
bewertet. Die `sum`-Zuweisung außerhalb der Marker darf nicht
verändert werden.
