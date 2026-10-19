# Zähler

Ein **Zähler** addiert an jeder Flanke eins zu seinem gespeicherten Wert und durchläuft 0, 1, 2, … und läuft über. Er ist ein Register mit rückgekoppeltem +1-Addierer.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/24-counter.html" title="Interaktiver mehrbittiger sequentieller Block, taktgesteuert" loading="lazy"></iframe></div>

Links schlägt ein freilaufender Takt; der 4-Bit-Zustand erscheint als Reihe von **Q-Lampen** und als Live-Wert (dezimal + binär). An jeder Flanke aktualisiert sich der Zustand; dazwischen hält er.

In VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= q + 1;   -- increment every edge
  end if;
end process;
```

Zähler messen Zeit und Ereignisse, erzeugen Adressen und teilen Takte. Sieh das unterste Bit an jeder Flanke kippen und jedes höhere Bit mit halbem Takt — binäres Zählen in Bewegung.
