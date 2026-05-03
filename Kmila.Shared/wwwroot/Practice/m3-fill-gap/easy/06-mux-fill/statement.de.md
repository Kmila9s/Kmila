## 2-zu-1-Multiplexer — fülle die Lücke

Ein 2-zu-1-Multiplexer leitet je nach Auswahlbit `sel` einen von
zwei Eingängen zum Ausgang.

* Bei `sel = '0'` folgt `y` dem Eingang `a`.
* Bei `sel = '1'` folgt `y` dem Eingang `b`.

Entity, Ports und das Architektur-Gerüst sind bereits vorhanden.
Implementiere die **einzelne nebenläufige Zuweisung** mit einem
`when ... else`-Ausdruck.

### Wahrheitstabelle

| `sel` | `a` | `b` | `y` |
|:-----:|:---:|:---:|:---:|
|   0   |  0  |  X  |  0  |
|   0   |  1  |  X  |  1  |
|   1   |  X  |  0  |  0  |
|   1   |  X  |  1  |  1  |

### Wo schreiben

Nur die Zeile zwischen `-- BEGIN_EDIT` und `-- END_EDIT` wird
bewertet. Alles andere ist gesperrtes Gerüst.
