## FIFO leer/voll-Flags

Implementiere den Belegungszähler einer FIFO der Tiefe 4. Du gibst nur `full` und `empty` aus — der Datenpfad wird nicht modelliert.

### Spezifikation

- 3-Bit-`unsigned`-Zähler `cnt` von 0..4.
- Pro Takt:
  - nur `push='1'` und `cnt < 4` → `cnt <= cnt + 1`.
  - nur `pop='1'` und `cnt > 0` → `cnt <= cnt - 1`.
  - sonst Wert halten.
- `empty <= '1'` bei `cnt = 0`.
- `full <= '1'` bei `cnt = 4`.

### Hinweis

`to_unsigned(4, 3)` für den Vergleich; an den Grenzen einklemmen.
