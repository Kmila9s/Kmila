## 4-Bit-Synchronzähler — Lücke füllen

Ein 4-Bit-Zähler ist **fast** fertig. Der Reset-Pfad, die Flankenerkennung und die Ausgabe-Zuweisung stehen schon.

Implementiere die **eine Zeile**, die den Zähler bei jeder steigenden Flanke weiterzählt.

### Spezifikation

* Asynchroner Reset: `q ← 0000`
* Bei `rising_edge(clk)` (außerhalb Reset): `q ← q + 1`
* Läuft von `1111` zurück auf `0000`

### Wohin schreiben

Nur die Zeile zwischen den Kommentaren `-- BEGIN_EDIT` und `-- END_EDIT` darf geändert werden. Der Rest ist das Entity-Gerüst, das der Bewerter erwartet.
