# Korrigieren: Der versehentliche Latch

Das ist einer der zwei oder drei Bugs, den jeder VHDL-Student mindestens einmal schreibt. Der Code sieht auf dem Papier korrekt aus, baut ohne Fehler, simuliert mit einer plausiblen Ausgabe — und wenn du ihn synthetisierst, warnt dich das Tool, dass es einen **Latch** abgeleitet hat, den du nie verlangt hast.

Die Ironie ist, dass Einheit IV dir beibringt, Latches (SR, D) *absichtlich* zu bauen. Die Fertigkeit hier ist zu erkennen, wann du einen **versehentlich** gebaut hast, mitten in etwas, das reine kombinatorische Logik sein sollte.

## Der Starter

```vhdl
architecture rtl of latch_bug is
begin
  process(enable, a, b)
  begin
    if enable = '1' then
      y <= a and b;
    end if;
    -- Fehlend: was soll y sein, wenn enable = '0'?
  end process;
end architecture rtl;
```

## Was schiefgeht

In einem rein kombinatorischen Prozess muss jeder Ausgang **auf jedem Pfad** durch den Prozess zugewiesen werden. Dieser hier weist `y` nur zu, wenn `enable = '1'`. Wenn `enable = '0'`, sagt der Prozess nichts über `y`.

VHDLs Antwort auf „du hast mir nicht gesagt, was ich mit diesem Signal tun soll“ lautet: **behalte den vorherigen Wert**. Die einzige Hardware, die einen vorherigen Wert behalten kann, ist ein Speicherelement — also leitet die Synthese einen **transparenten Latch** ab. Das ist genau ein D-Latch: solange `enable = '1'` folgt der Ausgang `a and b`; wenn `enable` fällt, wird der letzte Wert eingefroren. Du hast ein Gatter verlangt und ein Latch mit Enable bekommen.

## Warum ein versehentlicher Latch schlecht ist

Es ist nicht so, dass Latches böse wären — es ist, dass ein *ungewollter* dein Design ruiniert:

- **Timing ist schwer zu schließen.** Die statische Timing-Analyse kämpft mit transparenten Latches; die meisten FPGA-Flows gehen von flanken-getriggerten Flip-Flops aus und behandeln abgeleitete Latches aus gutem Grund als Warnung.
- **Glitches kommen durch.** Solange er transparent ist, fließt jeder Glitch auf `a` oder `b` direkt zu `y` und kann eingerastet werden.
- **Sim/Synthese-Mismatch-Risiko.** Das Verhalten hängt von subtiler Zuweisungsabdeckung ab, die leicht falsch zu machen ist.

Wenn das Tool also sagt „abgeleiteter Latch für Signal `y`“, behandle es als Fehler zum Beheben, nicht als Warnung zum Ignorieren.

## Dein Auftrag

Öffne das Beispiel und mach das Design **rein kombinatorisch** — kein Latch, kein Flip-Flop, nur Gatter. Zwei idiomatische Fixes:

**Fix 1: Default-Zuweisung oben**

```vhdl
process(enable, a, b)
begin
  y <= '0';                       -- Default: deckt jeden Pfad ab
  if enable = '1' then
    y <= a and b;
  end if;
end process;
```

Ein Signal darf in einem Prozessdurchlauf mehrfach zugewiesen werden — nur die letzte, die ausgeführt wird, wirkt. Der Default oben bedeutet „außer etwas weiter unten überschreibt mich, bin ich `'0'`“. Das ist die skalierbare Angewohnheit: setze zuerst Defaults, dann überschreibe.

**Fix 2: Vervollständige das `if`**

```vhdl
process(enable, a, b)
begin
  if enable = '1' then
    y <= a and b;
  else
    y <= '0';                     -- der fehlende Zweig
  end if;
end process;
```

Beide sind richtig. Beide produzieren identische Hardware (ein 2-Eingang-AND, getort durch `enable`, Ausgang `'0'` wenn deaktiviert). Nimm, was sich besser liest.

## Dieselbe Falle in einem `case`

Latches sind nicht nur ein `if`-Problem. Ein unvollständiges `case` oder ein Zweig, der einen Ausgang vergisst, leitet ebenfalls einen Latch ab:

```vhdl
-- BUG: 'y' ist für sel = "10" und "11" unzugewiesen
case sel is
  when "00" => y <= a;
  when "01" => y <= b;
  when others => null;   -- null weist nichts zu -> Latch auf y
end case;
```

Dieselbe Kur: gib `y` einen Default vor dem `case`, oder weise es in `when others` zu (und in jedem Zweig). `process(all)` hilft dir, den *Sensitivitätslisten*-Fehler zu vermeiden, aber es rettet dich **nicht** vor dem Abdeckungsfehler — du musst trotzdem jeden Ausgang auf jedem Pfad zuweisen.

## Verifiziere den Fix

Nach deinem Fix lass die Sim laufen. In der Wellenform:

- Für `15–25 µs` (wenn `enable = '1'` und `a = b = '1'`): `y = '1'`.
- Zu jeder anderen Zeit: `y = '0'`.

Vor dem Fix wäre `y` `'1'` geblieben, wenn `enable` zurück auf `'0'` fiel — das ist der Latch, der seinen vorherigen Wert hält, genau das, was du zu vermeiden versuchst.

## Was du behalten solltest

- In einem kombinatorischen Prozess muss **jeder Ausgang auf jedem Pfad zugewiesen werden** — ein `if`/`case`, das einen Pfad verpasst, leitet einen transparenten Latch ab.
- Eine Default-Zuweisung oben im Prozess ist der einfachste Weg, Abdeckung zu garantieren.
- Ein *versehentlicher* Latch verursacht Timing- und Glitch-Probleme; ein *absichtlicher* (D/SR-Latch) ist eine bewusste Designentscheidung aus Einheit IV.
- `process(all)` behebt Sensitivitätslisten, nicht Zuweisungsabdeckung — du musst trotzdem jeden Ausgang zuweisen.
- Wenn du wirklich Speicher willst, lass ihn sich nicht einschleichen — wechsle zu einem getakteten Prozess und steh zu der Entscheidung.
