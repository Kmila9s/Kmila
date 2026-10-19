# ALU — Arithmetisch-logische Einheit

Die **ALU** ist der Rechner im Herzen einer CPU: sie nimmt zwei Operanden A und B und erzeugt, per **Operationsauswahl** gewählt, A+B, A−B, A UND B, A ODER B und mehr.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/17-alu.html" title="Interaktive ALU mit Operations-Wahltrommel" loading="lazy"></iframe></div>

Die Wahltrommel am Modul wählt die Operation; die Taster setzen **A** und **B**. Die Liste hebt die aktuelle Operation hervor und die Anzeige zeigt **A op B = R**. Rein kombinatorisch — ohne Takt.

In VHDL:

```vhdl
with op select
  r <= a + b     when "00",   -- ADD
       a - b     when "01",   -- SUB
       a and b   when "10",   -- AND
       a or b    when others; -- OR
```

Eine ALU ist **kombinatorische Logik** (Addierer, die Gatter und MUXe, die das Ergebnis wählen) ohne eigenen Speicher — die Register ringsum halten Operanden und Ergebnis. Die Operationsauswahl ist nur ein MUX über die Kandidaten-Ergebnisse.
