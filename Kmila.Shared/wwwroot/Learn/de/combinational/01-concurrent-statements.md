# Nebenläufige Zuweisungen: `when/else` und `with/select`

Kombinatorische Logik wird in VHDL mit nebenläufigen Anweisungen geschrieben — jede Zeile außerhalb eines Prozesses beschreibt ein Stück fest verdrahteter Hardware. Für dieselbe Aufgabe gibt es zwei Syntaxvarianten: die **konditionale** und die **selektive** Zuweisung.

## `when/else` — konditional

Gut, wenn du eine priorisierte Liste von Bedingungen hast oder wenn jeder Zweig ein anderes Signal prüft:

```vhdl
y <= '0' when reset = '1' else
     a   when mode  = "read" else
     b   when mode  = "write" else
     'Z';
```

Die erste zutreffende Bedingung gewinnt. Die Reihenfolge zählt — es kompiliert zu einem **Prioritäts-Mux**: Die oberste Bedingung hat den direktesten Pfad zu `y`.

## `with/select` — selektiv

Gut, wenn du auf einen einzelnen Ausdruck umschaltest (so wie es eine Software-`switch`-Anweisung tut):

```vhdl
with mode select
  y <=
    a   when "00",
    b   when "01",
    c   when "10",
    'Z' when others;
```

Alle Fälle werden gegen dasselbe Signal (`mode`) verglichen, und `when others` ist zwingend, um jedes mögliche Muster abzudecken. Dieser Stil kompiliert zu einem **balancierten Mux** — kein Zweig ist bevorzugt.

## Welche solltest du wählen?

- **Gleiches Signal, viele Muster:** `with/select` liest sich wie eine Wahrheitstabelle. Nimm es.
- **Verschiedene Signale, Prioritätslogik:** `when/else`. Passt natürlich.
- **Synthese-Ergebnis:** Beide erzeugen auf einem modernen FPGA etwa die gleichen Gatter — die klarere Form im Quelltext zu wählen ist fast immer die richtige Entscheidung.

## Eine kleine Falle

Beide Formen verlangen, dass `y` in **jedem** Szenario zugewiesen wird. Wenn du das abschließende `else` bei `when/else` oder das `when others` bei `with/select` vergisst, warnt dich der Compiler — und der Synthesizer kann ein Latch schlussfolgern, um den letzten Wert zu "merken", was fast nie gewollt ist. Das ist dieselbe Latch-Falle, die du in der Fix-it-Übung des sequentiellen Moduls wiedersehen wirst.

## Wo das als Nächstes auftaucht

Du wirst `with/select` gleich im 3-zu-8-Decoder-Beispiel nach dieser Lektion verwenden und `when/else` überall in der späteren Ampel-FSM. Beide sind VHDL-Brot-und-Butter.
