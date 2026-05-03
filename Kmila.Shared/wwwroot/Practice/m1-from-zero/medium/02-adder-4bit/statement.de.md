## 4-Bit-Addierer

Implementiere einen 4-Bit-Addierer mit Eingangsübertrag `cin`. Berechne `sum = a + b + cin` und den Ausgangsübertrag `cout`.

### Spezifikation

- Eingänge: `a` und `b` sind 4-Bit-`STD_LOGIC_VECTOR`; `cin` ist ein Bit.
- Ausgänge: `sum` mit 4 Bit; `cout` mit 1 Bit.

### Hinweis

Verwende `IEEE.NUMERIC_STD.ALL` und ein 5-Bit-`unsigned`-Hilfssignal:

```vhdl
signal s : unsigned(4 downto 0);
s    <= ('0' & unsigned(a)) + ('0' & unsigned(b)) + ("0000" & cin);
sum  <= std_logic_vector(s(3 downto 0));
cout <= s(4);
```
