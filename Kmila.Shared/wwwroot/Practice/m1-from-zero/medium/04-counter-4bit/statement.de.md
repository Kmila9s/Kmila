## 4-Bit-Synchronzähler

Baue einen 4-Bit-Zähler, der bei jeder steigenden Taktflanke inkrementiert. Ein asynchrones, aktiv-hohes `reset` setzt den Zähler auf `0000`.

### Spezifikation

- Intern: `count : unsigned(3 downto 0) := (others => '0')`.
- Bei `rising_edge(clk)`: `count <= count + 1;`.
- Ausgabe: `q <= std_logic_vector(count);`.

### Hinweis

Verwende `IEEE.NUMERIC_STD.ALL`, damit `+ 1` auf `unsigned` funktioniert.
