## T-Flipflop — Toggle-Zeile ergänzen

Ein T-Flipflop ist **fast** fertig: Reset, Flankenerkennung und die
Ausgabe `q <= qi` sind bereits vorhanden.

Implementiere das Toggle-Verhalten im steigenden Taktzweig:

* falls `t = '1'` → `qi` wechselt
* falls `t = '0'` → `qi` bleibt gleich

### Spezifikation

* Asynchroner Reset: `qi ← '0'`.
* Bei `rising_edge(clk)`: `qi ← qi xor t`.

### Wo schreiben

Nur zwischen `-- BEGIN_EDIT` und `-- END_EDIT` editieren. Reset und
Ausgabe sind gesperrt.

### Hinweis

`qi <= qi xor t;`
