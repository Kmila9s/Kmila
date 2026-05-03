## 4-Wort-Stack-Flags

Implementiere den Belegungszähler eines LIFO-Stacks der Tiefe 4. Es werden nur `full` und `empty` ausgegeben.

### Spezifikation

- 3-Bit-`unsigned`-Zähler `cnt` von 0..4.
- Pro Takt:
  - nur `push='1'` und `cnt < 4` → `cnt <= cnt + 1`.
  - nur `pop='1'` und `cnt > 0` → `cnt <= cnt - 1`.
  - sonst halten.
- `empty <= '1'` bei `cnt = 0`.
- `full <= '1'` bei `cnt = 4`.

### Hinweis

Die Steuerung entspricht einer FIFO der Tiefe 4 — nur die Datenreihenfolge unterscheidet sich, was hier nicht modelliert ist.
