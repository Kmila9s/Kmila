# XOR-Gatter (Exklusiv-ODER)

Das **XOR**-Gatter (Exklusiv-ODER) gibt 1 aus, wenn seine Eingänge **verschieden** sind.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/08-xor.html" title="Interaktives Logikgatter auf einem Modul-Prüfstand" loading="lazy"></iframe></div>

Bewege den Cursor (oder die Pfeiltasten), um jede Eingangskombination durchzugehen. Das Modul berechnet das Ergebnis und die Ausgangslampe **Y** leuchtet gemäß der Tabelle.

In VHDL:

```vhdl
y <= a xor b;
```

Es ist rein **kombinatorisch**: der Ausgang hängt nur von den aktuellen Eingängen ab, ohne Takt und ohne Speicher. XOR ist das Herz der Addition (das Summenbit), der Parität und des Vergleichs: es beantwortet „sind diese zwei Bits verschieden?".
