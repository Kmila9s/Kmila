# Demultiplexer (DEMUX)

Ein **Demultiplexer** ist die Umkehrung des MUX: er leitet einen Eingang **D** zu einem von mehreren Ausgängen, gewählt durch die **Auswahl**leitung S.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/11-demux.html" title="Interaktiver Schaltnetz-Block auf einem Modul-Prüfstand" loading="lazy"></iframe></div>

Bewege den Cursor (oder die Pfeiltasten), um jede Eingangskombination durchzugehen. Die Taster setzen die Bits, das Modul rechnet, die Ausgangslampen leuchten und die **aktuelle Tabellenzeile wird hervorgehoben**, sodass du die Funktion direkt abliest.

In VHDL:

```vhdl
y0 <= d when s = '0' else '0';
y1 <= d when s = '1' else '0';
```

Es ist rein **kombinatorisch**: die Ausgänge hängen nur von den aktuellen Eingängen ab, ohne Takt und ohne Speicher. Die nicht gewählten Ausgänge bleiben 0. Dekodierer und Demultiplexer sind verwandt: ein Demux ist ein Dekodierer, dessen Dateneingang den aktiven Ausgang freigibt.
