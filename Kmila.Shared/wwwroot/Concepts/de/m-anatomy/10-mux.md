# Multiplexer (MUX)

Ein **Multiplexer** leitet einen von mehreren Dateneingängen zum Ausgang. Eine **Auswahl**leitung **S** bestimmt welchen: hier Y = D1 wenn S = 1, sonst D0.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/10-mux.html" title="Interaktiver Schaltnetz-Block auf einem Modul-Prüfstand" loading="lazy"></iframe></div>

Bewege den Cursor (oder die Pfeiltasten), um jede Eingangskombination durchzugehen. Die Taster setzen die Bits, das Modul rechnet, die Ausgangslampen leuchten und die **aktuelle Tabellenzeile wird hervorgehoben**, sodass du die Funktion direkt abliest.

In VHDL:

```vhdl
y <= d1 when s = '1' else d0;
```

Es ist rein **kombinatorisch**: die Ausgänge hängen nur von den aktuellen Eingängen ab, ohne Takt und ohne Speicher. Jedes `if/then/else` und `case` in VHDL wird nach der Synthese ein MUX. So funktioniert auch das Load-Enable eines Registers: ein MUX wählt zwischen neuem Wert und Halten des alten.
