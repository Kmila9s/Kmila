## 2-Bit-Addierer — Bug finden und beheben

Diese Schaltung soll zwei 2-Bit-Zahlen `a` und `b` plus den
Eingangsübertrag `cin` addieren und liefert eine 2-Bit-`sum` und
einen finalen `cout`. Der Autor hat jedes Ausgangsbit als
unabhängigen Halbaddierer geschrieben und **vergessen, den
Übertrag** von Bit 0 nach Bit 1 weiterzuleiten.

Finde den Fehler und verschalte die Überträge richtig.

### Spezifikation

* `{cout, sum} = a + b + cin` (vorzeichenlos, 3 Bit Ergebnis).

### Hinweis

Bit 1 der Summe muss einen möglichen Übertrag aus Bit 0
berücksichtigen.
