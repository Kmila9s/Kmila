## Ladbares Register — falsche Taktflanke

Verlangt war ein 4-Bit-Register mit synchroner Übernahme.

### Spezifikation

* Asynchroner High-aktiver `reset` setzt `q` auf `"0000"`.
* Bei `rising_edge(clk)`, wenn `load = '1'`, dann `q <- d`. Sonst `q` halten.

### Was schiefläuft

Das Register taktet an der **fallenden** statt an der steigenden Flanke. Daten erscheinen einen halben Takt zu spät, das Zeitmodell ist falsch.

### Hinweis

`falling_edge(clk)` und `rising_edge(clk)` sind Spiegelbilder. Prüfe, welche der Process verwendet.
