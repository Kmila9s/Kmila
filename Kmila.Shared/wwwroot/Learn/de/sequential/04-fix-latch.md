# Fix it: das versehentliche Latch

Das ist einer der zwei oder drei Bugs, die jeder VHDL-Studierende mindestens einmal schreibt. Der Code sieht auf dem Papier korrekt aus, baut ohne Fehler, simuliert mit plausibler Ausgabe — und wenn du ihn synthetisierst, warnt dich das Tool, dass es ein **Latch** hergeleitet hat, das du nie wolltest.

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

In einem rein kombinatorischen Prozess muss jeder Ausgang **auf jedem Pfad** durch den Prozess zugewiesen werden. Dieser weist `y` nur dann zu, wenn `enable = '1'`. Wenn `enable = '0'`, sagt der Prozess nichts über `y`.

VHDLs Lösung für "du hast mir nicht gesagt, was ich mit diesem Signal tun soll": **merke den vorherigen Wert**. Das ist ein Latch — ein transparentes Speicherelement. Die Synthese leitet eines her, dein Design hat einen Timing-Pfad-Albtraum, und das Board glitcht bei Signalübergängen.

## Deine Mission

Öffne das Beispiel und mache das Design **rein kombinatorisch** — kein Latch, kein Flipflop, nur Gatter. Zwei idiomatische Lösungen:

**Fix 1: Standardzuweisung am Anfang**

```vhdl
process(enable, a, b)
begin
  y <= '0';                       -- Standard
  if enable = '1' then
    y <= a and b;
  end if;
end process;
```

Ein Signal kann in einem Prozess mehrfach zugewiesen werden — nur die letzte Zuweisung wirkt. Der Standard am Anfang bedeutet "sofern mich nichts überschreibt, sei `'0'`".

**Fix 2: Das `if` vervollständigen**

```vhdl
process(enable, a, b)
begin
  if enable = '1' then
    y <= a and b;
  else
    y <= '0';
  end if;
end process;
```

Beide sind richtig. Beide erzeugen identische Hardware. Wähle, was sich für das Design, an dem du arbeitest, besser liest.

## Den Fix verifizieren

Nach deinem Fix, führe die Sim aus. In der Waveform:

- Für `15–25 µs` (wenn `enable = '1'` und `a=b='1'`): `y = '1'`.
- Zu jeder anderen Zeit: `y = '0'`.

Vor dem Fix würde `y` auf `'1'` bleiben, wenn `enable` auf `'0'` zurückfällt — das ist das Latch, das seinen vorherigen Wert hält, was genau das ist, was du vermeiden willst.

## Die Regel

In einem kombinatorischen Prozess muss **jeder Ausgang auf jedem Pfad zugewiesen werden**. Wenn du das nicht erfüllen kannst, füge entweder einen Standard hinzu, oder du willst tatsächlich ein Flipflop — in welchem Fall du zu einem getakteten Prozess wechseln und diese Entscheidung bewusst treffen solltest.
