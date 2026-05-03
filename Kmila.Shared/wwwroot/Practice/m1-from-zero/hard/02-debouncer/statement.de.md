## Tasten-Entpreller

Filtere ein verrauschtes Tasten-Signal. Der Ausgang `clean` wechselt erst, wenn `raw` für **N = 4** aufeinanderfolgende Takte stabil war.

### Spezifikation

- Halte den letzten Abtastwert von `raw` und einen 2-Bit-Zähler.
- Bei jedem Takt: ist `raw == last`, inkrementiere; bei Sättigung `"11"` übernimmt der Ausgang `raw`.
- Wenn `raw` wechselt: Zähler zurücksetzen und `last` aktualisieren.
- Reset löscht alle internen Signale und setzt `clean = '0'`.

### Hinweis

`unsigned(1 downto 0)` für den Zähler, ein `STD_LOGIC` für den stabilen Pegel.
