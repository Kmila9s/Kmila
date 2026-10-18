# Komparator

Ein **Komparator** vergleicht zwei Zahlen A und B und meldet den Fall: A < B, A = B oder A > B.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/16-comparator.html" title="Interaktiver Schaltnetz-Block auf einem Modul-Prüfstand" loading="lazy"></iframe></div>

Bewege den Cursor (oder die Pfeiltasten), um jede Eingangskombination durchzugehen. Die Taster setzen die Bits, das Modul rechnet, die Ausgangslampen leuchten und die **aktuelle Tabellenzeile wird hervorgehoben**, sodass du die Funktion direkt abliest.

In VHDL:

```vhdl
lt <= '1' when a < b else '0';
eq <= '1' when a = b else '0';
gt <= '1' when a > b else '0';
```

Es ist rein **kombinatorisch**: die Ausgänge hängen nur von den aktuellen Eingängen ab, ohne Takt und ohne Speicher. Genau einer der drei Ausgänge ist 1. Betragskomparatoren wachsen Bit für Bit, vom höchstwertigen Bit abwärts.
