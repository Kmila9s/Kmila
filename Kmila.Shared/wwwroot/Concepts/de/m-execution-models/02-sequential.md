# Sequenziell: ein Schritt nach dem anderen

Innerhalb eines `process`-Körpers verhält sich VHDL sehr ähnlich wie
die Sprachen, die du schon kennst: Anweisungen laufen **von oben nach
unten, in Quelltextreihenfolge**. Verschiebst du eine Zeile, änderst du
das Ergebnis.

Aber es gibt einen Haken, der jeden erwischt: **Variablen** und
**Signale** verhalten sich unterschiedlich.

| Art | Syntax | Wann erscheint der neue Wert? |
|---|---|---|
| **Variable** | `tmp := a + 1` | **Sofort.** Die nächste Anweisung liest das neue `tmp`. |
| **Signal** | `result <= tmp + b` | **Am Ende des Delta-Zyklus.** Vorher sehen Leseoperationen noch den alten Wert. |

Der Player geht durch einen winzigen `process`, der beides benutzt.
Achte darauf, wie `tmp` in dem Moment aktualisiert wird, in dem wir
Zeile 5 ausführen, aber `result` sich erst *nach* dem Verlassen des
`process` ändert.

## Warum das wichtig ist

Stell dir vor, du hättest geschrieben:

```vhdl
result <= 1;
result <= 2;
```

In C wäre `result` nach diesen zwei Zeilen ganz klar `2`. In VHDL?
**Die letzte Zuweisung gewinnt** — aber erst, wenn der Delta schließt.
Solange der `process` läuft, ist `result` noch sein alter Wert, und
der Simulator verwirft die `<= 1`-Transaktion stillschweigend, weil
`<= 2` sie ersetzt, bevor der Delta endet.

> **Probier's:** geh zu Zeile 7 und schau in die Signaltabelle. `tmp`
> ist `8`, aber `result` ist immer noch `U`. Geh einen Schritt weiter
> und sieh zu, wie `result` *nach* dem Ende des `process`-Körpers auf
> `13` springt.
