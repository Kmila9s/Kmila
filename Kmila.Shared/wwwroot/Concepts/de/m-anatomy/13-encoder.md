# Kodierer

Ein **Kodierer** ist die Umkehrung des Dekodierers: aus einem One-Hot-Eingang (eine Leitung aktiv) gibt er die **Binäradresse** dieser Leitung aus.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/13-encoder.html" title="Interaktiver Schaltnetz-Block auf einem Modul-Prüfstand" loading="lazy"></iframe></div>

Bewege den Cursor (oder die Pfeiltasten), um jede Eingangskombination durchzugehen. Die Taster setzen die Bits, das Modul rechnet, die Ausgangslampen leuchten und die **aktuelle Tabellenzeile wird hervorgehoben**, sodass du die Funktion direkt abliest.

In VHDL:

```vhdl
a <= "11" when i3 = '1' else
     "10" when i2 = '1' else
     "01" when i1 = '1' else
     "00";
```

Es ist rein **kombinatorisch**: die Ausgänge hängen nur von den aktuellen Eingängen ab, ohne Takt und ohne Speicher. Ein **Prioritäts**kodierer löst mehrere aktive Eingänge auf, indem er den höchsten nimmt — nützlich für Interrupt-Controller, die eine Anforderung wählen müssen.
