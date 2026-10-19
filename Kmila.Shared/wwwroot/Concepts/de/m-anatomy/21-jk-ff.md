# JK-Flip-Flop

Ein **JK-Flip-Flop** ist das allgemeinste: an jeder Flanke setzt J Q auf 1, K auf 0, beide zusammen **kippen** es, keines hält. Es ist das erste **sequentielle** Element des Pfads: anders als ein Gatter hängt sein Ausgang vom Takt und von seiner eigenen Vergangenheit ab, nicht nur von den aktuellen Eingängen.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/21-jk-ff.html" title="Interaktives sequentielles Element mit Live-Zeitdiagramm" loading="lazy"></iframe></div>

Dieses Element hat **Speicher**. Beobachte das Zeitdiagramm: der Takt (**CLK**) schlägt oben, und der Ausgang **Q** ändert sich nur am Takt — dazwischen **hält** er seinen Wert, egal was die Eingänge tun. Q ist farbig gezeichnet; die gestrichelten Linien markieren die Taktflanken.

In VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if    j='1' and k='1' then q <= not q;
    elsif j='1' then q <= '1';
    elsif k='1' then q <= '0';
    end if;
  end if;
end process;
```

Das JK beseitigt den verbotenen Zustand des alten SR-Latches. Jedes Flip-Flop lässt sich aus einem anderen bauen — D, T und JK sind mit etwas Logik davor austauschbar.
