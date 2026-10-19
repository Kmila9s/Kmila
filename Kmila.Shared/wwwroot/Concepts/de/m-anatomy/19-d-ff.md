# D-Flip-Flop

Ein **D-Flip-Flop** speichert ein Bit, übernommen an der **steigenden Flanke** des Takts. Q nimmt den Wert von D im Moment der Flanke und hält ihn bis zur nächsten. Es ist das erste **sequentielle** Element des Pfads: anders als ein Gatter hängt sein Ausgang vom Takt und von seiner eigenen Vergangenheit ab, nicht nur von den aktuellen Eingängen.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/19-d-ff.html" title="Interaktives sequentielles Element mit Live-Zeitdiagramm" loading="lazy"></iframe></div>

Dieses Element hat **Speicher**. Beobachte das Zeitdiagramm: der Takt (**CLK**) schlägt oben, und der Ausgang **Q** ändert sich nur am Takt — dazwischen **hält** er seinen Wert, egal was die Eingänge tun. Q ist farbig gezeichnet; die gestrichelten Linien markieren die Taktflanken.

In VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= d;          -- sampled on the edge
  end if;
end process;
```

Das D-Flip-Flop ist **der** Baustein des synchronen Entwurfs — jedes Register, jede Pipeline-Stufe und jedes Zustandsbit ist eines oder mehrere DFFs. Setz einen MUX vor D und du hast Load-Enable oder synchronen Reset.
