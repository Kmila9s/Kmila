## 4-Bit-vorzeichenloser Multiplizierer

Kombinatorischer Multiplizierer: `product = a * b`, mit `a`/`b` 4 Bit und `product` 8 Bit.

### Spezifikation

- Rein kombinatorisch — kein Takt.
- `IEEE.NUMERIC_STD` verwenden. `*` ist für `unsigned` überladen.
- Die natürliche Breite von `unsigned(3 downto 0) * unsigned(3 downto 0)` ist genau 8 Bit.

### Hinweis

Eine Zeile:

```vhdl
product <= std_logic_vector(unsigned(a) * unsigned(b));
```
