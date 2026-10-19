# Register

Ein **Register** ist eine Bank von Flip-Flops, die ein ganzes Wort auf einmal speichert. An der Taktflanke (beim Laden) werden alle Bits von D gemeinsam in Q übernommen.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/22-register.html" title="Interaktiver mehrbittiger sequentieller Block, taktgesteuert" loading="lazy"></iframe></div>

Links schlägt ein freilaufender Takt; der 4-Bit-Zustand erscheint als Reihe von **Q-Lampen** und als Live-Wert (dezimal + binär). An jeder Flanke aktualisiert sich der Zustand; dazwischen hält er.

In VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if ld = '1' then q <= d; end if;  -- load on edge
  end if;
end process;
```

In Registern hält eine CPU ihre Arbeitswerte, und jede Pipeline-Stufe ist ein Register. Setz einen MUX davor (neuer Wert vs. aktuelles Q) und das Load-Enable wird zum Halten.
