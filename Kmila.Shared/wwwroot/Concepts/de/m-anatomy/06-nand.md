# NAND-Gatter

Das **NAND**-Gatter ist ein UND gefolgt von einem NICHT: es gibt 0 nur aus, wenn beide Eingänge 1 sind.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/06-nand.html" title="Interaktives Logikgatter auf einem Modul-Prüfstand" loading="lazy"></iframe></div>

Bewege den Cursor (oder die Pfeiltasten), um jede Eingangskombination durchzugehen. Das Modul berechnet das Ergebnis und die Ausgangslampe **Y** leuchtet gemäß der Tabelle.

In VHDL:

```vhdl
y <= a nand b;
```

Es ist rein **kombinatorisch**: der Ausgang hängt nur von den aktuellen Eingängen ab, ohne Takt und ohne Speicher. Die Blase am Ausgang markiert die Inversion. **NAND ist universell**: jede Logikfunktion lässt sich allein aus NAND-Gattern aufbauen.
