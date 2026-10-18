# Puffer (BUF)

Der **Puffer** kopiert seinen Eingang unverändert an den Ausgang (Y = A). Er ändert die Logik nicht — er dient dazu, ein Signal zu **treiben**: es zu verstärken oder eine kontrollierte Verzögerung hinzuzufügen.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/03-buffer.html" title="Interaktives Logikgatter auf einem Modul-Prüfstand" loading="lazy"></iframe></div>

Bewege den Cursor (oder die Pfeiltasten), um jede Eingangskombination durchzugehen. Das Modul berechnet das Ergebnis und die Ausgangslampe **Y** leuchtet gemäß der Tabelle.

In VHDL:

```vhdl
y <= a;
```

Es ist rein **kombinatorisch**: der Ausgang hängt nur von den aktuellen Eingängen ab, ohne Takt und ohne Speicher. Beachte: am Ausgang gibt es keine Inversionsblase — das ist der einzige Unterschied zu einem NICHT-Gatter.
