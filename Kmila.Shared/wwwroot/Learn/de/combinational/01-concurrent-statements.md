# Nebenläufige Zuweisungen: `when/else` und `with/select`

Kombinatorische Logik in VHDL wird mit nebenläufigen Anweisungen geschrieben — jede Zeile außerhalb eines Prozesses beschreibt ein Stück fest verdrahtete Hardware. Zwei Syntax-Varianten existieren für denselben Job: **bedingte** und **selektive** Zuweisung.

## `when/else` — bedingt

Gut, wenn du eine priorisierte Liste von Bedingungen hast oder wenn jeder Zweig ein anderes Signal prüft:

```vhdl
y <= '0' when reset = '1' else
     a   when mode  = "read" else
     b   when mode  = "write" else
     'Z';
```

Die erste passende Bedingung gewinnt. Die Reihenfolge zählt — es compiliert zu einem **Prioritäts-MUX**: die oberste Bedingung hat den direktesten Pfad zu `y`.

## `with/select` — selektiv

Gut, wenn du über einen einzelnen Ausdruck schaltest (so wie ein Software-`switch`):

```vhdl
with mode select
  y <=
    a   when "00",
    b   when "01",
    c   when "10",
    'Z' when others;
```

Alle Fälle vergleichen gegen dasselbe Signal (`mode`), und `when others` ist Pflicht, um jedes mögliche Muster abzudecken. Dieser Stil compiliert zu einem **balancierten MUX** — kein Zweig wird bevorzugt.

## Welche wählen?

- **Gleiches Signal, viele Muster:** `with/select` liest sich wie eine Wahrheitstabelle. Nimm das.
- **Verschiedene Signale, Prioritätslogik:** `when/else`. Natürlich.
- **Syntheseausgabe:** beide produzieren auf modernen FPGAs etwa dieselben Gatter — die im Quellcode klarere Form ist fast immer die richtige Wahl.

## Eine kleine Falle

Beide Formen verlangen, dass `y` in **jedem** Szenario zugewiesen wird. Vergisst du das finale `else` bei `when/else` oder lässt `when others` bei `with/select` weg, warnt der Compiler — und der Synthesizer kann einen Latch ableiten, um den letzten Wert zu „merken", was fast nie gewünscht ist. Es ist dieselbe Latch-Falle, die du in der Fix-it-Übung des sequentiellen Moduls wiedersiehst.

## Wo das als nächstes auftaucht

Du nutzt `with/select` direkt im 3-zu-8-Decoder-Sample nach dieser Lektion und `when/else` überall im Ampel-FSM später. Beides ist VHDL-Brot-und-Butter.
