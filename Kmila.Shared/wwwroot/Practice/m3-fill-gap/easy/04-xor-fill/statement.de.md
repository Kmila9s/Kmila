## XOR-Gatter — fülle die Lücke

Entity, Ports und das Architektur-Gerüst sind bereits vorhanden.
Implementiere die **einzelne nebenläufige Zuweisung**, die `y` mit
dem exklusiven ODER von `a` und `b` versorgt (wahr, wenn die
Eingänge sich unterscheiden).

### Wahrheitstabelle

| `a` | `b` | `y` |
|:---:|:---:|:---:|
|  0  |  0  |  0  |
|  0  |  1  |  1  |
|  1  |  0  |  1  |
|  1  |  1  |  0  |

### Wo schreiben

Nur die Zeile zwischen `-- BEGIN_EDIT` und `-- END_EDIT` wird
bewertet. Alles andere ist gesperrtes Gerüst.

### Tipp

Ein Operator: `xor`.
