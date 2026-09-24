## LFSR — repariere das Polynom

Dieser 8-Bit-LFSR soll eine pseudozufällige Sequenz maximaler
Länge erzeugen (Periode 255). Der aktuelle Code verwendet ein
nicht-primitives Polynom: nur zwei Taps werden XOR-verknüpft, das
Register fällt in einen kurzen Zyklus und die Ausgabe ist alles
andere als zufällig.

### Spezifikation

* Synchroner, high-aktiver Reset; Reset-Wert `00000001`.
* Fibonacci-LFSR mit Linksshift, neues Bit an Position 0:
  `reg <= reg(6 downto 0) & fb`.
* Rückkopplung `fb` = XOR der Taps **8, 6, 5, 4** (1-indiziert),
  also `reg(7) xor reg(5) xor reg(4) xor reg(3)`. Polynom:
  `x^8 + x^6 + x^5 + x^4 + 1`.

### Hinweis

Zwei-Tap-Polynome über GF(2) sind bei Länge 8 fast nie primitiv.
Der kanonische Tap-Satz für einen 8-Bit-Maximal-LFSR ist
`{8, 6, 5, 4}`.
