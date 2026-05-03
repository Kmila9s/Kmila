## Schieberegister — Füllbit-Auswahl

Ein 8-Bit-Linksschieberegister ist verdrahtet: Der Reset löscht es,
und bei jeder steigenden Taktflanke schiebt das Register und nimmt
rechts ein einzelnes Füllbit `feed` auf.

Du musst die Auswahl von `feed` implementieren:

* `mode = '1'` → `feed` folgt `din`
* `mode = '0'` → `feed` wird auf `'0'` gezwungen

### Spezifikation

* Asynchroner Reset: `sreg ← 0000_0000`
* Bei `rising_edge(clk)`: `sreg ← sreg(6..0) & feed`
* `q` spiegelt `sreg` jederzeit wider.

### Wo schreiben

Nur zwischen `-- BEGIN_EDIT` und `-- END_EDIT` editieren. Der Schiebe-
Prozess, der Reset und die Ausgabe sind gesperrt.

### Hinweis

Eine einzige nebenläufige Zuweisung mit `when … else` reicht.
