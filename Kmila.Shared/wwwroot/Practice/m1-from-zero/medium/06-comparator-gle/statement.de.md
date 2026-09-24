## Größer-/Gleich-/Kleiner-Vergleicher

Vergleiche zwei vorzeichenlose 4-Bit-Werte `a` und `b` und treibe drei Flags:

- `gt = 1`, wenn `a > b`
- `eq = 1`, wenn `a = b`
- `lt = 1`, wenn `a < b`

Genau ein Flag ist zu jedem Zeitpunkt `1`.

### Hinweis

Verwende `IEEE.NUMERIC_STD.ALL` und die konkurrente Bedingungsform:

```vhdl
gt <= '1' when unsigned(a) > unsigned(b) else '0';
```
