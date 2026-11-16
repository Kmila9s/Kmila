**Dein Ziel:** Baue einen Zähler, der bei jeder steigenden Taktflanke **eins** addiert. Lege den Block `count <= count + 1` in `on rising_edge(clk)` und drücke **Ausführen** — der Player taktet ihn viermal.

Probier auch die Experimente: ein Inkrement **außerhalb** des Blocks (gar kein Takt) oder **zwei** Inkremente darin. Das Urteil erklärt, was in VHDL wirklich passiert.
