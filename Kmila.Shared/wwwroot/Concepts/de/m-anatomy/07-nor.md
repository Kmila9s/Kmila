# NOR-Gatter

Das **NOR**-Gatter ist ein ODER gefolgt von einem NICHT: es gibt 1 nur aus, wenn beide Eingänge 0 sind.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/07-nor.html" title="Interaktives Logikgatter auf einem Modul-Prüfstand" loading="lazy"></iframe></div>

Bewege den Cursor (oder die Pfeiltasten), um jede Eingangskombination durchzugehen. Das Modul berechnet das Ergebnis und die Ausgangslampe **Y** leuchtet gemäß der Tabelle.

In VHDL:

```vhdl
y <= a nor b;
```

Es ist rein **kombinatorisch**: der Ausgang hängt nur von den aktuellen Eingängen ab, ohne Takt und ohne Speicher. Die Blase markiert die Inversion. **NOR ist ebenfalls universell**: jede Schaltung lässt sich allein aus NOR-Gattern aufbauen.
