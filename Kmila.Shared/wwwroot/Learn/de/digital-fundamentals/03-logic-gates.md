# Die sieben Logikgatter

Jede digitale Schaltung, bis hinunter zu einer modernen GPU, ist eine Kombination aus sieben elementaren Zellen. Wenn du weißt, wie sich jede einzelne verhält und wie du sie in VHDL schreibst, hast du schon mit dem Hardwaredesign angefangen.

## Die sechs Zwei-Eingangs-Gatter

| Gatter | Symbol in VHDL | Liest sich als | Ausgang ist `1`, wenn… |
|--------|----------------|----------------|------------------------|
| AND    | `a and b`      | "A und B"       | beide Eingänge 1 sind |
| OR     | `a or b`       | "A oder B"      | mindestens ein Eingang 1 ist |
| XOR    | `a xor b`      | "A xor B"       | die Eingänge unterschiedlich sind |
| NAND   | `a nand b`     | "nicht (A und B)" | mindestens ein Eingang 0 ist |
| NOR    | `a nor b`      | "nicht (A oder B)" | beide Eingänge 0 sind |
| XNOR   | `a xnor b`     | "A = B"         | die Eingänge gleich sind |

Und das Ein-Eingangs-NOT:

```vhdl
y <= not a;
```

## Warum NAND und NOR speziell sind

Ein klassisches Ergebnis der Digitallogik: **NAND allein** (oder NOR allein) reicht, um jedes andere Gatter zu bauen. Beide werden genau aus diesem Grund "funktional vollständig" genannt. Deshalb liefern CMOS-Prozessbibliotheken viele NAND2- und NOR2-Zellen — aus ihnen kann man alles synthetisieren.

- NOT: `not a = a nand a`
- AND: `a and b = not (a nand b)`
- OR:  `a or b = (not a) nand (not b)`

## XOR ist das mathematische

XOR wird oft mit einem eingekreisten Plus (⊕) gezeichnet. Es ist das Gatter, das "Parität" berechnet — sein Ausgang ist `1`, wenn eine ungerade Anzahl von Eingängen `1` ist. Das macht es zum Kern von:

- Addierern: `sum = a xor b xor carry_in`
- Paritätsprüfern und Fehlererkennungscodes
- Toggle-Flipflops: `q_next = q xor toggle_input`

## Alle sechs in einer Datei sehen

Wenn du das **Wahrheitstabellen-Explorer**-Lab für dieses Modul öffnest, findest du eine einzige VHDL-Entity, die alle sechs 2-Eingangs-Gatter gleichzeitig berechnet, mit den Eingängen `a` und `b`, die für Stimulus an den Ports-Tab angeschlossen sind. Der Waveform-Viewer zeigt dann die Reaktion jedes Ausgangs bei jeder Kombination.

```vhdl
architecture rtl of gates is
begin
  o_and  <= a and b;
  o_or   <= a or  b;
  o_xor  <= a xor b;
  o_nand <= a nand b;
  o_nor  <= a nor  b;
  o_xnor <= a xnor b;
end architecture rtl;
```

Beachte, dass jede dieser sechs Zeilen **ein Draht** ist, keine Anweisung, die läuft. Sechs Gatter, sechs Stücke Hardware — sie werden alle gleichzeitig ausgewertet.

## Was zu merken ist

- Sechs Zwei-Eingangs-Gatter + NOT sind der komplette Satz.
- NAND und NOR sind "universell" — du kannst alles aus ihnen bauen.
- In VHDL sind Gatter nebenläufige Einzeilen-Zuweisungen. Sie werden nicht "aufgerufen"; sie existieren.
