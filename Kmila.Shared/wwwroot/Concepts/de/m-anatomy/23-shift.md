# Schieberegister

Ein **Schieberegister** schiebt seine Bits an jeder Flanke um eine Stelle weiter, ein neues Bit tritt an einem Ende ein — eine Eimerkette für Daten.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/23-shift.html" title="Interaktiver mehrbittiger sequentieller Block, taktgesteuert" loading="lazy"></iframe></div>

Links schlägt ein freilaufender Takt; der 4-Bit-Zustand erscheint als Reihe von **Q-Lampen** und als Live-Wert (dezimal + binär). An jeder Flanke aktualisiert sich der Zustand; dazwischen hält er.

In VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= q(2 downto 0) & si;  -- shift left, SI in
  end if;
end process;
```

Schieberegister machen Seriell-Parallel-Wandlung (UART-Empfang), multiplizieren/dividieren durch zwei und bauen LFSRs für pseudozufällige Folgen.
