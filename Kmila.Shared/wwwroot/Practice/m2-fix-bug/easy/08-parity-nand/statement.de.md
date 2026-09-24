## 4-Bit-Parität — Bug finden und beheben

Diese Schaltung soll die **Parität** des 4-Bit-Vektors `d`
ausgeben: `p = 1`, wenn eine ungerade Zahl von Bits in `d` `1`
sind, sonst `p = 0`. Der Autor hat stattdessen NAND-Gatter
verkettet — und liegt damit daneben.

Finde den Fehler und verwende den richtigen Operator.

### Spezifikation

* `p = d(0) ⊕ d(1) ⊕ d(2) ⊕ d(3)`

### Hinweis

Parität liefert genau XOR — eine NAND-Kette tut das nicht.
