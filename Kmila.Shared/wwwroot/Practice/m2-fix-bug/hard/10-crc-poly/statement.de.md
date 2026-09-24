## CRC-8 — repariere das Polynom

Dieser serielle CRC-8-Generator XOR-t ein Rückkopplungsbit in
bestimmte Schieberegister-Positionen, die durch das Polynom
gewählt werden. Der aktuelle Code verdrahtet das falsche Polynom:
`0x07 = x^2 + x + 1` statt `0x1D = x^4 + x^3 + x^2 + 1`. Die
berechneten CRCs stimmen nicht mit der Erwartung überein.

### Spezifikation

* Synchroner, high-aktiver Reset; `crc` startet bei `00000000`.
* Jeder Takt: `fb = reg(7) xor din`.
* `reg(0) <= fb`, und für das Polynom `0x1D`:
  * `reg(2) <= reg(1) xor fb`
  * `reg(3) <= reg(2) xor fb`
  * `reg(4) <= reg(3) xor fb`
  * die übrigen Bits schieben unverändert weiter.

### Hinweis

Die XOR-Taps folgen den Koeffizienten von `0x1D` (binär
`00011101`). Die fehlerhafte Version XOR-t nur an Position 1 und
2, was `0x07` entspricht.
