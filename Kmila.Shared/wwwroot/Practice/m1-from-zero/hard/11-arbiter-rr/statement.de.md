## Round-Robin-Arbiter (4)

Implementiere einen 4-Kanal-Round-Robin-Arbiter. Pro Takt genau eine Anforderung gewähren — `grant` ist one-hot (oder komplett null, wenn keine `req`-Bits gesetzt sind).

### Spezifikation

- Halte `last`, einen 2-Bit-Index des letzten Gewinners (nach Reset 0).
- Bei jeder steigenden Flanke `req` ab `last + 1` durchsuchen (2-Bit-Wraparound).
- Das erste gesetzte Bit gewinnt: dieses Bit in `grant` setzen, `last` aktualisieren.
- Sind keine `req`-Bits gesetzt, bleibt `grant = 0000` und `last` unverändert.

### Hinweis

`for i in 0 to 3 loop` mit einer Schutzvariable, um beim ersten Treffer abzubrechen. `to_integer(idx)` indiziert den Vektor.
