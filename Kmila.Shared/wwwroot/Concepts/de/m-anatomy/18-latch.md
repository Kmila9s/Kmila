# D-Latch

Ein **D-Latch** speichert ein Bit. Solange sein Enable (hier der Takt) hoch ist, ist es **transparent** — Q folgt D; geht es auf low, **friert** es den letzten Wert ein. Es ist das erste **sequentielle** Element des Pfads: anders als ein Gatter hängt sein Ausgang vom Takt und von seiner eigenen Vergangenheit ab, nicht nur von den aktuellen Eingängen.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/18-latch.html" title="Interaktives sequentielles Element mit Live-Zeitdiagramm" loading="lazy"></iframe></div>

Dieses Element hat **Speicher**. Beobachte das Zeitdiagramm: der Takt (**CLK**) schlägt oben, und der Ausgang **Q** ändert sich nur am Takt — dazwischen **hält** er seinen Wert, egal was die Eingänge tun. Q ist farbig gezeichnet; die gestrichelten Linien markieren die Taktflanken.

In VHDL:

```vhdl
process (en, d)
begin
  if en = '1' then   -- transparent while enabled
    q <= d;
  end if;
end process;
```

Weil es **pegelgesteuert** ist, ist ein Latch eine ganze Taktphase lang transparent — jeder Glitch auf D in diesem Fenster geht durch. Darum bevorzugt synchroner Entwurf flankengesteuerte Flip-Flops.
