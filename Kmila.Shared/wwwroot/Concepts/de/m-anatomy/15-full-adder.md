# Volladdierer

Ein **Volladdierer** addiert drei Bits — A, B und einen Eingangsübertrag **Ci** — und erzeugt Summe S und Ausgangsübertrag **Co**. Verkettet addieren sie breite Zahlen.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/15-full-adder.html" title="Interaktiver Schaltnetz-Block auf einem Modul-Prüfstand" loading="lazy"></iframe></div>

Bewege den Cursor (oder die Pfeiltasten), um jede Eingangskombination durchzugehen. Die Taster setzen die Bits, das Modul rechnet, die Ausgangslampen leuchten und die **aktuelle Tabellenzeile wird hervorgehoben**, sodass du die Funktion direkt abliest.

In VHDL:

```vhdl
s  <= a xor b xor ci;
co <= (a and b) or (ci and (a xor b));
```

Es ist rein **kombinatorisch**: die Ausgänge hängen nur von den aktuellen Eingängen ab, ohne Takt und ohne Speicher. Der Volladdierer ist der Baustein jedes Binäraddierers: verkette n davon, Übertrag an Übertrag, und du addierst zwei n-Bit-Zahlen. Er besteht aus zwei Halbaddierern und einem ODER.
