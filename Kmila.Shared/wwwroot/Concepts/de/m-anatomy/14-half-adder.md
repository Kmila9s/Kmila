# Halbaddierer

Ein **Halbaddierer** addiert zwei Bits A und B und erzeugt eine **Summe** S und einen **Übertrag** C.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/14-half-adder.html" title="Interaktiver Schaltnetz-Block auf einem Modul-Prüfstand" loading="lazy"></iframe></div>

Bewege den Cursor (oder die Pfeiltasten), um jede Eingangskombination durchzugehen. Die Taster setzen die Bits, das Modul rechnet, die Ausgangslampen leuchten und die **aktuelle Tabellenzeile wird hervorgehoben**, sodass du die Funktion direkt abliest.

In VHDL:

```vhdl
s <= a xor b;   -- sum
c <= a and b;   -- carry
```

Es ist rein **kombinatorisch**: die Ausgänge hängen nur von den aktuellen Eingängen ab, ohne Takt und ohne Speicher. Die Summe ist ein **XOR**, der Übertrag ein **UND**: ein Halbaddierer sind buchstäblich diese zwei Gatter nebeneinander. Er hat keinen Eingangsübertrag, ist also nur die erste Stufe.
