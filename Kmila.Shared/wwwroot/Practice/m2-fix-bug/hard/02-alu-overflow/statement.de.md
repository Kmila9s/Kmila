## ALU — repariere das Vorzeichen-Overflow-Flag

Die 8-Bit-ALU berechnet `a + b` im Zweierkomplement und aktiviert
`ovf`, wenn das signierte Ergebnis nicht in 8 Bit passt. Die aktuelle
Implementierung ist "fast richtig" — sie hat die gleiche Form wie
die Lehrbuchformel, liest aber das falsche Bit.

### Spezifikation

* Eingänge `a`, `b`: 8-Bit signed (Zweierkomplement).
* Sei `sum = a + b` (auf 8 Bit gekürzt).
* Signed-Overflow tritt genau dann auf, wenn beide Operanden das
  gleiche Vorzeichen haben und das Ergebnis das entgegengesetzte
  Vorzeichen.
* Formal: `ovf = (a[7] = b[7]) AND (sum[7] /= a[7])`.

### Hinweis

Bit 7 ist das Vorzeichen-Bit im 8-Bit-Zweierkomplement. Jedes andere
Bit zu lesen macht die Formel mathematisch sinnlos.
