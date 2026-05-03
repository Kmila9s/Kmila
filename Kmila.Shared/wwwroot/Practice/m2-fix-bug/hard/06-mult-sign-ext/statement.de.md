## 4×4-Multiplizierer — repariere die asymmetrische Vorzeichenerweiterung

Zwei vorzeichenlose 4-Bit-Werte `a` und `b` werden multipliziert
und ergeben ein vorzeichenloses 8-Bit-`product`. Der aktuelle Code
interpretiert `a` versehentlich als **signed**, während `b`
unsigned bleibt — das liefert falsche Werte, sobald `a(3) = '1'`
(also `a >= 8`).

### Spezifikation

* `a`, `b` sind **unsigned** 4-Bit-Zahlen im Bereich `0..15`.
* `product = a * b` als **unsigned** 8-Bit interpretiert
  (Bereich `0..225`).

### Hinweis

Beide Operanden gleich casten: `unsigned(a) * unsigned(b)`. Das
Ergebnis passt genau in 8 Bit — `resize` ist nicht nötig.
