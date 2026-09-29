# Projekt: münzbetriebener Verkaufsautomat

Das ist dein erstes richtiges Projekt. Die Spec ist fest, der Starter-Code reicht dir die Verkabelung, und dein Job ist es, die Logik zu schreiben, die ihn vollendet.

## Was du baust

Einen FSM, der das von einem Kunden eingeworfene Geld verfolgt und ein Produkt ausgibt (einen einzelnen Output-Puls, `dispense`), wenn die Summe 25 ¢ erreicht. Die Eingänge sind:

- `nickel` — 1-Zyklus-Puls: „eine 5-¢-Münze ist gerade reingefallen“
- `dime`   — 1-Zyklus-Puls: „eine 10-¢-Münze ist gerade reingefallen“
- `clk`, `reset`

Und die Ausgänge:

- `dispense` — für genau einen Taktzyklus high, wenn du ≥ 25 ¢ erreicht hast
- `change`   — gleichzeitig mit `dispense` für einen Zyklus high, wenn die Summe **mehr** als 25 ¢ war (Maschine muss Wechselgeld zurückgeben)

## Akzeptanzkriterien

| Münzsequenz          | Erwartetes Verhalten          |
|----------------------|-------------------------------|
| 5 Nickel             | `dispense=1, change=0`        |
| 2 Dimes + 1 Nickel   | `dispense=1, change=0`        |
| 3 Dimes (= 30 ¢)     | `dispense=1, change=1`        |
| 2 Nickel + 1 Dime    | kein Dispense (nur 20 ¢)      |

## Was der Starter dir gibt

Die Entity und der `total`-Akkumulator sind verkabelt. Jeder `nickel`-Puls addiert 5; jeder `dime`-Puls addiert 10. Aber der Starter treibt immer `dispense = '0'` und `change = '0'`. **Das ist es, was du ändern musst.**

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

## Empfohlene Vorgehensweise

1. **Erkenne die Verkaufsbedingung.** Bei jeder steigenden Taktflanke prüfen, ob `total >= 25` *nach* dem Addieren der aktuellen Münze ist. (Vorsicht: du musst die *neue* Summe berechnen, nicht die aus dem vorigen Zyklus.)
2. **Pulse `dispense` für genau einen Zyklus.** Trick: wenn du den Verkauf erkennst, setze ein Flag-Signal und setze `total` auf null zurück. Das Flag lebt einen Zyklus.
3. **Berechne `change`.** Wenn die Summe im Verkaufsmoment > 25 war, pulse auch `change`.

Hinweis: einfacher, wenn du die „nächste Summe“ als lokales Signal (oder `variable` im Prozess) berechnest:

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

## Weitergehen

- Füge einen **Refund**-Knopf hinzu, der `total` ohne Verkauf nullt.
- Unterstütze eine **Quarter**-Münze (25 ¢) — soll sofort ausgeben.
- Verfolge die Anzahl verkaufter Artikel in einem separaten 8-Bit-Zähler, der als Ausgang sichtbar ist. So eine Rückmeldung schicken echte Verkaufsautomaten nach oben.
