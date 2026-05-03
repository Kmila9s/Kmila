## 3-Bit-Zähler mit Wrap — fülle die Lücke

Ein synchroner 3-Bit-Zähler ist **fast** fertig. Reset, die Erkennung
der Taktflanke und die Zuweisung an `q` sind bereits verdrahtet.

Implementiere den Rumpf des `rising_edge(clk)`-Zweigs, sodass der
Zähler `000 → 001 → … → 111 → 000` durchläuft, also nach sieben
sauber wieder bei null beginnt.

### Spezifikation

* Asynchroner Reset: `count ← 000`
* Bei `rising_edge(clk)`:
  * falls `count = 111` → `count ← 000`
  * sonst → `count ← count + 1`
* `q` spiegelt `count` jederzeit wider.

### Wo schreiben

Nur zwischen `-- BEGIN_EDIT` und `-- END_EDIT` editieren. Alles andere
ist festes Gerüst, das der Grader unverändert erwartet.

### Hinweis

Vergleiche `count` mit `to_unsigned(7, 3)` (oder `"111"`) und wähle
zwischen `(others => '0')` und `count + 1`.
