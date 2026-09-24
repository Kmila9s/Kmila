## SPI-Master (Shift)

Implementiere einen minimalen SPI-Master, der bei `start='1'` ein Byte MSB-first ausschiebt.

### Spezifikation

- Ein 4-Bit-Phasenzähler `phase` taktet die Aktion; im aktiven Zustand inkrementiert er pro Takt.
- `sclk = phase(0)` solange aktiv — ergibt 8 Hoch/Tief-Paare.
- `mosi = shreg(7)` solange aktiv — also das aktuelle MSB.
- Schiebe `shreg` nach links, wenn `phase(0) = '1'`.
- Reset löscht alles. Nach 16 Phasen wird wieder inaktiv (die Tests prüfen nur das frühe Fenster).

### Hinweis

Ein `active`-Flag hält die Ausgänge im Idle auf `'0'`.
