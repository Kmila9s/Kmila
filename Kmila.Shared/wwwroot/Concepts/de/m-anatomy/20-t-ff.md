# T-Flip-Flop

Ein **T-Flip-Flop schaltet um**: an jeder Flanke kippt das gespeicherte Bit (0↔1), wenn T = 1; bei T = 0 wird gehalten. Es halbiert die Taktfrequenz. Es ist das erste **sequentielle** Element des Pfads: anders als ein Gatter hängt sein Ausgang vom Takt und von seiner eigenen Vergangenheit ab, nicht nur von den aktuellen Eingängen.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/20-t-ff.html" title="Interaktives sequentielles Element mit Live-Zeitdiagramm" loading="lazy"></iframe></div>

Dieses Element hat **Speicher**. Beobachte das Zeitdiagramm: der Takt (**CLK**) schlägt oben, und der Ausgang **Q** ändert sich nur am Takt — dazwischen **hält** er seinen Wert, egal was die Eingänge tun. Q ist farbig gezeichnet; die gestrichelten Linien markieren die Taktflanken.

In VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if t = '1' then q <= not q; end if;
  end if;
end process;
```

Verkette T-Flip-Flops und jedes halbiert erneut — genau das ist ein binärer **Ripple-Zähler**. Ein T ist ein D, dessen D mit Q xor T verbunden ist.
