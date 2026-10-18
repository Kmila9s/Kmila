# Dekodierer

Ein **Dekodierer** macht aus einer n-Bit-Adresse einen **One-Hot**-Ausgang: genau einer seiner 2ⁿ Ausgänge ist 1, der von der Adresse benannte.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/12-decoder.html" title="Interaktiver Schaltnetz-Block auf einem Modul-Prüfstand" loading="lazy"></iframe></div>

Bewege den Cursor (oder die Pfeiltasten), um jede Eingangskombination durchzugehen. Die Taster setzen die Bits, das Modul rechnet, die Ausgangslampen leuchten und die **aktuelle Tabellenzeile wird hervorgehoben**, sodass du die Funktion direkt abliest.

In VHDL:

```vhdl
with a select y <=
  "0001" when "00",
  "0010" when "01",
  "0100" when "10",
  "1000" when others;
```

Es ist rein **kombinatorisch**: die Ausgänge hängen nur von den aktuellen Eingängen ab, ohne Takt und ohne Speicher. Dekodierer treiben Adressleitungen: Speicher, Siebensegmentanzeigen und Befehlsverteilung wählen so eine Leitung aus vielen.
