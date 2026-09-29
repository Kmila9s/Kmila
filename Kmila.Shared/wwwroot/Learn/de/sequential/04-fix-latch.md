# Korrigieren: Der versehentliche Latch

Das ist einer der zwei oder drei Bugs, den jeder VHDL-Student mindestens einmal schreibt. Der Code sieht auf dem Papier richtig aus, baut ohne Fehler, simuliert mit einer plausiblen Ausgabe — und wenn du synthetisierst, warnt das Tool, dass es einen **Latch** abgeleitet hat, den du nie wolltest.

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

VHDLs Lösung für „du hast mir nicht gesagt, was ich mit diesem Signal tun soll“: **merke dir den vorherigen Wert**. Das ist ein Latch — ein transparentes Speicherelement. Die Synthese leitet einen ab, dein Design hat einen Timing-Pfad-Albtraum, und das Board glitcht bei Signalübergängen.

## Dein Auftrag

Öffne das Beispiel und mach das Design **rein kombinatorisch** — kein Latch, kein Flip-Flop, nur Gatter. Zwei idiomatische Fixes:

**Fix 1: Default-Zuweisung oben**

```vhdl
process(enable, a, b)
begin
  y <= '0';                       -- Default
  if enable = '1' then
    y <= a and b;
  end if;
end process;
```

Ein Signal darf in einem Prozess mehrfach zugewiesen werden — nur die letzte Zuweisung gilt. Der Default oben bedeutet „außer etwas überschreibt mich, bin ich `'0'`“.

**Fix 2: Vervollständige das `if`**

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

Beide sind richtig. Beide produzieren identische Hardware. Nimm, was sich für dein Design besser liest.

## Verifiziere den Fix

Nach deinem Fix lass die Sim laufen. In der Wellenform:

- Für `15–25 µs` (wenn `enable = '1'` und `a=b='1'`): `y = '1'`.
- Zu jeder anderen Zeit: `y = '0'`.

Vor dem Fix wäre `y` `'1'` geblieben, wenn `enable` zurück auf `'0'` fiel — das ist der Latch, der seinen vorherigen Wert hält, was du genau vermeiden willst.

## Die Regel

In einem kombinatorischen Prozess **muss jeder Ausgang auf jedem Pfad zugewiesen werden**. Wenn du das nicht erfüllen kannst, füge entweder einen Default hinzu, oder du willst eigentlich ein Flip-Flop — dann wechsle zu einem getakteten Prozess und stehe zu der Entscheidung.
