# Projekt: münzbetriebener Verkaufsautomat

Das ist dein erstes richtiges Projekt. Die Spezifikation ist fest, der Starter-Code gibt dir die Verdrahtung, und deine Aufgabe ist es, die Logik zu schreiben, die es vervollständigt.

## Was du baust

Eine FSM, die das Geld verfolgt, das ein Kunde eingeworfen hat, und ein Produkt ausgibt (ein einzelner Ausgangspuls, `dispense`), wenn die Summe 25 ¢ erreicht. Die Eingänge sind:

- `nickel` — ein 1-Zyklen-Puls, der bedeutet "eine 5-¢-Münze ist gerade eingeworfen worden"
- `dime`   — ein 1-Zyklen-Puls, der bedeutet "eine 10-¢-Münze ist gerade eingeworfen worden"
- `clk`, `reset`

Und die Ausgänge:

- `dispense` — geht für genau einen Taktzyklus hoch, wenn du ≥ 25 ¢ erreicht hast
- `change`   — geht für einen Zyklus zusammen mit `dispense` hoch, wenn die Summe **mehr** als 25 ¢ war (damit die Maschine Geld zurückgeben muss)

## Akzeptanzkriterien

| Münzfolge            | Erwartetes Verhalten          |
|----------------------|-------------------------------|
| 5 Nickels            | `dispense=1, change=0`        |
| 2 Dimes + 1 Nickel   | `dispense=1, change=0`        |
| 3 Dimes (= 30 ¢)     | `dispense=1, change=1`        |
| 2 Nickels + 1 Dime   | keine Ausgabe (nur 20 ¢)      |

## Was dir der Starter gibt

Die Entity und der `total`-Akkumulator sind verdrahtet. Jeder `nickel`-Puls addiert 5; jeder `dime`-Puls addiert 10. Aber der Starter treibt `dispense = '0'` und `change = '0'` immer. **Das ist, was du ändern musst.**

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    total <= (others => '0');
  elsif rising_edge(clk) then
    if    nickel = '1' then total <= total + 5;
    elsif dime   = '1' then total <= total + 10;
    end if;
  end if;
end process;

dispense <= '0';  -- TODO
change   <= '0';  -- TODO
```

## Vorgeschlagener Ansatz

1. **Erkenne die Verkaufsbedingung.** Prüfe bei jeder steigenden Taktflanke, ob `total >= 25` ist, nachdem die aktuelle Münze addiert wurde. (Vorsicht: Du musst die *neue* Summe berechnen, nicht die aus dem vorherigen Zyklus.)
2. **Pulsiere `dispense` für genau einen Zyklus.** Der Trick: Wenn du den Verkauf erkennst, setze ein Flag-Signal und setze `total` zurück auf null. Das Flag lebt für einen Zyklus.
3. **Berechne `change`.** Wenn die Summe im Verkaufsmoment > 25 war, pulsiere auch `change`.

Tipp: Es ist einfacher, wenn du die "nächste Summe" als lokales Signal (oder `variable` im Prozess) berechnest:

```vhdl
variable next_total : unsigned(5 downto 0);
...
next_total := total;
if    nickel = '1' then next_total := total + 5;
elsif dime   = '1' then next_total := total + 10;
end if;

if next_total >= 25 then
  dispense <= '1';
  change   <= '1' when next_total > 25 else '0';
  total    <= (others => '0');
else
  dispense <= '0';
  change   <= '0';
  total    <= next_total;
end if;
```

## Noch weiter

- Füge eine **Erstattungs**-Taste hinzu, die `total` auf null setzt, ohne auszugeben.
- Unterstütze eine **Quarter**-Münze (25 ¢) — sollte sofort ausgeben.
- Verfolge die Anzahl verkaufter Artikel in einem separaten 8-Bit-Zähler, sichtbar als Ausgang. Das ist die Art von Rückmeldung, die echte Verkaufsautomaten nach oben senden.
