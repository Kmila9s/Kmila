# Projekt: münzbetriebener Verkaufsautomat

Das ist dein erstes richtiges Projekt. Die Spec ist fest, der Starter-Code reicht dir die Verkabelung, und dein Job ist es, die Logik zu schreiben, die ihn vollendet.

## Was du baust

Einen Zustandsautomaten, der das von einem Kunden eingeworfene Geld verfolgt und ein Produkt ausgibt (einen einzelnen Output-Puls, `dispense`), wenn die Summe 25 ¢ erreicht. Die Eingänge sind:

- `nickel` – 1-Zyklus-Puls: „eine 5-¢-Münze ist gerade reingefallen"
- `dime`   – 1-Zyklus-Puls: „eine 10-¢-Münze ist gerade reingefallen"
- `clk`, `reset`

Und die Ausgänge:

- `dispense` – für genau einen Taktzyklus high, wenn du ≥ 25 ¢ erreicht hast
- `change`   – gleichzeitig mit `dispense` für einen Zyklus high, wenn die Summe **mehr** als 25 ¢ war (die Maschine muss also Wechselgeld zurückgeben)

## Zwei Wege, das zu modellieren

Ein Verkaufsautomat ist ein klassischer Zustandsautomat, und es gibt zwei ehrliche Wege, ihn zu beschreiben:

- **Explizite Zustände.** Zähle jede erreichbare Summe auf – `S0, S5, S10, S15, S20` – und zeichne für jede Münze einen Übergang. Das ist der Lehrbuch-Ansatz und das, was du von Hand für einen kleinen Münzsatz machen würdest. Er macht das Zustandsdiagramm offensichtlich, explodiert aber, sobald du mehr Münzen oder einen höheren Preis hinzufügst.
- **Akkumulator- (Datenpfad-)Stil.** Halte die Summe in einem kleinen Register und vergleiche mit dem Preis. Weniger Zeilen, skaliert auf jeden Preis, und das ist die Form, die der Starter-Code nutzt. Du kannst dir das `total`-Register als den Zustand und den Vergleicher als die Folgezustandslogik vorstellen.

So oder so hängt `dispense` von der Münze ab, die *in diesem* Zyklus ankommt, verhält sich also wie ein **Mealy**-Ausgang – es reagiert im selben Zyklus, in dem die Verkaufsbedingung wahr wird, statt einen Zyklus zu warten.

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

1. **Erkenne die Verkaufsbedingung.** Bei jeder steigenden Taktflanke prüfen, ob `total >= 25` *nach* dem Addieren der aktuellen Münze ist. (Vorsicht: Du musst die *neue* Summe berechnen, nicht die aus dem vorigen Zyklus.)
2. **Pulse `dispense` für genau einen Zyklus.** Der Trick: Wenn du den Verkauf erkennst, setze den Puls und setze `total` auf null zurück. Weil `total` an derselben Flanke gelöscht wird, ist die Bedingung im nächsten Zyklus falsch, der Puls ist also von Natur aus einen Zyklus breit.
3. **Berechne `change`.** Wenn die Summe im Verkaufsmoment > 25 war, pulse auch `change`.

Berechne die „nächste Summe" als `variable` innerhalb des Prozesses, damit die Entscheidung den aktuellen Wert nutzt, nicht das Register vom letzten Zyklus:

```vhdl
process(clk, reset)
  variable next_total : unsigned(5 downto 0);
begin
  if reset = '1' then
    total    <= (others => '0');
    dispense <= '0';
    change   <= '0';
  elsif rising_edge(clk) then
    next_total := total;
    if    nickel = '1' then next_total := total + 5;
    elsif dime   = '1' then next_total := total + 10;
    end if;

    if next_total >= 25 then
      dispense <= '1';
      if next_total > 25 then change <= '1'; else change <= '0'; end if;
      total    <= (others => '0');
    else
      dispense <= '0';
      change   <= '0';
      total    <= next_total;
    end if;
  end if;
end process;
```

Zwei Dinge sind zu beachten. Eine `variable` wird mit `:=` zugewiesen und aktualisiert sich **sofort** innerhalb des Prozesses, weshalb `next_total` die Münze dieses Zyklus bereits enthält – ein Signal (`<=`) würde erst aktualisiert, wenn der Prozess suspendiert. Und weil `dispense`, `change` und `total` alle innerhalb des getakteten Prozesses getrieben werden, sind sie **registriert** – die Pulse richten sich an den Taktflanken aus und glitchen nicht.

## Darauf musst du achten

- **Breite.** `total + 10` kann 30 erreichen, bevor der Reset feuert, also dimensioniere das Register auf mindestens 6 Bit – `unsigned(5 downto 0)` hält 0–63 mit Luft nach oben. Ein zu schmaler Vektor läuft stillschweigend über.
- **Beide Münzen gleichzeitig.** Das `if … elsif` bedeutet, dass ein gleichzeitiges `nickel` und `dime` nur den Nickel zählt. Entscheide, ob das für deine Spec akzeptabel oder ein Fehler ist.
- **`total` nicht zu löschen vergessen.** Wenn du `dispense` pulst, aber den Akkumulator nicht nullst, „verkauft" die Maschine jeden Zyklus erneut, bis mehr Münzen die Summe ändern. Das Löschen an der Verkaufsflanke ist das, was den Puls einen Zyklus breit macht.

## Akzeptanzkriterien

| Münzsequenz          | Erwartetes Verhalten          |
|----------------------|-------------------------------|
| 5 Nickel             | `dispense=1, change=0`        |
| 2 Dimes + 1 Nickel   | `dispense=1, change=0`        |
| 3 Dimes (= 30 ¢)     | `dispense=1, change=1`        |
| 2 Nickel + 1 Dime    | kein Dispense (nur 20 ¢)      |

## Weitergehen

- Füge einen **Refund**-Knopf hinzu, der `total` ohne Verkauf nullt.
- Unterstütze eine **Quarter**-Münze (25 ¢) – soll sofort ausgeben.
- Verfolge die Anzahl verkaufter Artikel in einem separaten 8-Bit-Zähler, der als Ausgang sichtbar ist. So eine Rückmeldung schicken echte Verkaufsautomaten nach oben.

## Bezug zum Kurs

Das ist Einheit IIs *aplicaciones con autómatas y lenguaje de descripción de hardware*: eine deterministische sequentielle Maschine, aus einem Verhalten spezifiziert und dann in HDL beschrieben. Die Akkumulator-plus-Vergleicher-Form ist auch ein sanfter Einstieg in Einheit V, wo du einen Entwurf explizit in einen **Datenpfad** (das `total`-Register und den Addierer) und einen **Steuerautomaten**, der ihn treibt, aufteilst.

## Was du behalten solltest

- Ein Verkaufsautomat ist ein Zustandsautomat; modelliere ihn mit expliziten Zuständen für Klarheit oder einem Akkumulator-Register für Skalierbarkeit.
- Nutze eine `variable` (`:=`) für die „nächste Summe", damit die Verkaufsprüfung die gerade angekommene Münze einschließt.
- Erzeuge einen Ein-Zyklus-Puls, indem du den Akkumulator an der Verkaufsflanke löschst – registrierte Ausgänge halten den Puls sauber.
- Dimensioniere Vektoren für den größten Wert, den du erreichen kannst, und entscheide im Voraus, wie sich gleichzeitige Eingänge verhalten.
