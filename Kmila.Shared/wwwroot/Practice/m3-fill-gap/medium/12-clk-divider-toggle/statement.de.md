## Taktteiler — Toggle ergänzen

Ein 4-Bit-Taktteiler ist **fast** fertig: Der Modulo-10-Zähler
(`TOP = 9`), der Reset und die Ausgabe `clk_out <= clk_reg` sind
bereits vorhanden. Erreicht der Zähler `TOP`, wird er auf `0` gesetzt.

Du musst beschreiben, was **im selben Moment** mit `clk_reg` passiert:
Er soll umschalten. Außerhalb dieses Ereignisses bleibt `clk_reg`
unverändert.

### Spezifikation

* Asynchroner Reset: `count ← 0`, `clk_reg ← '0'`.
* Bei `rising_edge(clk)`:
  * falls `count = TOP` → `count ← 0`, `clk_reg ← not clk_reg`
  * sonst → `count ← count + 1`

### Wo schreiben

Nur zwischen `-- BEGIN_EDIT` und `-- END_EDIT` editieren. Zähler,
Reset und Ausgabe sind gesperrt.

### Hinweis

`clk_reg <= not clk_reg;`
