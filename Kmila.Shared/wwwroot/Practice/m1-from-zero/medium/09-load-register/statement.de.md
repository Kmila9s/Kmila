## Lade-Register (4 Bit)

Implementiere ein 4-Bit-Register mit synchronem Laden. Bei steigender Taktflanke wird `d` in das Register kopiert, falls `load = '1'`. Sonst bleibt der Wert erhalten. Ein asynchrones, aktiv-hohes `reset` setzt auf `0000`.

### Spezifikation

- Intern: `signal r : std_logic_vector(3 downto 0) := (others => '0');`.
- Prozess: `if load = '1' then r <= d;`.
- Ausgabe: `q <= r;`.

### Hinweis

Ein häufiger Baustein — gleiche Struktur wie ein Zähler, der neue Wert kommt aber aus `d` statt `count + 1`.
