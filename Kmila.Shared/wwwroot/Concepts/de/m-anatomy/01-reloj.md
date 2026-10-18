# Der Takt: der Herzschlag der Schaltung

Der **Takt** (*clock*) ist das Signal, das alles taktet: ein **Rechtecksignal**, das mit **fester Frequenz** zwischen 0 und 1 wechselt. Vor Gattern und Flip-Flops lohnt es sich, ihn in Bewegung zu sehen.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/01-reloj.html" title="Interaktiver Takt — ein Zahnstab, der ein Rechtecksignal zeichnet" loading="lazy"></iframe></div>

**Bewege den Cursor** nach links und rechts, um die Frequenz zu ändern. Der Zahnstab ist das Signal als Mechanik: der Taster steigt auf jeden Zahn (Pegel **1**) und fällt in jede Lücke (Pegel **0**), während das Schwungrad eine konstante Geschwindigkeit vorgibt. Die Anzeige zeigt Pegel und Frequenz.

## Worauf es wirklich ankommt: die Flanke

Sequentielle Logik — Flip-Flops, Register, Zähler — reagiert nicht auf den *Pegel* des Takts, sondern auf seine **Flanke**: den Moment des Sprungs von 0 auf 1 (**steigende Flanke**) oder 1 auf 0 (**fallende Flanke**). In VHDL:

```vhdl
process (clk)
begin
    if rising_edge(clk) then
        q <= d;          -- genau an der steigenden Flanke übernommen
    end if;
end process;
```

- **Frequenz** — Zyklen pro Sekunde (Hz). Schnelleres Schwungrad, engeres Signal.
- **Periode** — ein voller Zyklus: ein Zahn plus eine Lücke.
- **Tastverhältnis** — der Anteil der Periode auf 1; hier 50 %.
