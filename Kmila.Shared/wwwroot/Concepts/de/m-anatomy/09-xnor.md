# XNOR-Gatter (Äquivalenz)

Das **XNOR**-Gatter gibt 1 aus, wenn seine Eingänge **gleich** sind — es ist ein XOR mit Inversionsblase.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/09-xnor.html" title="Interaktives Logikgatter auf einem Modul-Prüfstand" loading="lazy"></iframe></div>

Bewege den Cursor (oder die Pfeiltasten), um jede Eingangskombination durchzugehen. Das Modul berechnet das Ergebnis und die Ausgangslampe **Y** leuchtet gemäß der Tabelle.

In VHDL:

```vhdl
y <= a xnor b;
```

Es ist rein **kombinatorisch**: der Ausgang hängt nur von den aktuellen Eingängen ab, ohne Takt und ohne Speicher. XNOR beantwortet „sind diese zwei Bits gleich?" — es ist die 1-Bit-Gleichheitsfunktion (Vergleicher).
