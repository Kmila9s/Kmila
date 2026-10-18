# UND-Gatter

Das **UND**-Gatter gibt 1 nur aus, wenn **beide** Eingänge 1 sind.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/04-and.html" title="Interaktives Logikgatter auf einem Modul-Prüfstand" loading="lazy"></iframe></div>

Bewege den Cursor (oder die Pfeiltasten), um jede Eingangskombination durchzugehen. Das Modul berechnet das Ergebnis und die Ausgangslampe **Y** leuchtet gemäß der Tabelle.

In VHDL:

```vhdl
y <= a and b;
```

Es ist rein **kombinatorisch**: der Ausgang hängt nur von den aktuellen Eingängen ab, ohne Takt und ohne Speicher. Denk an zwei Schalter in Reihe: Strom erreicht die Lampe nur, wenn beide geschlossen sind.
