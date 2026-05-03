## 8-Bit-Schieberegister

Baue ein 8-Bit-Schieberegister. Bei jeder steigenden Taktflanke wird der Inhalt um eine Position nach links verschoben und der serielle Eingang `din` in Bit 0 geladen. Ein asynchrones, aktiv-hohes `reset` löscht das Register.

### Spezifikation

- Internes Signal: `r : std_logic_vector(7 downto 0)`.
- Aktualisierung: `r <= r(6 downto 0) & din;`.
- Ausgabe: `q <= r;`.

### Hinweis

Verwende `process(clk, reset)` mit `if reset = '1' then ... elsif rising_edge(clk) then ...`.
