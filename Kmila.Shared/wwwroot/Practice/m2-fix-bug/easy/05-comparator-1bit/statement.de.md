## 4-Bit-Gleichheit — Bug finden und beheben

Diese Schaltung soll `eq = 1` ausgeben, wenn die 4-Bit-Vektoren
`a` und `b` gleich sind, sonst `0`. Sie funktioniert in vielen
Fällen — meldet aber einige Paare als „gleich", die es eindeutig
nicht sind.

Finde den Fehler und behebe ihn.

### Spezifikation

* `eq = 1` wenn `a = b`, sonst `0`.
* Alle vier Bits müssen am Vergleich teilnehmen.

### Hinweis

Prüfe den Indexbereich, der im Vergleich benutzt wird.
