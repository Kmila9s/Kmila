# Prozesse und Sensitivitätslisten

Alles in VHDL außerhalb eines Prozesses ist nebenläufig — Drähte und Gatter. In dem Moment, in dem du willst, dass eine Schaltung von einem Taktzyklus zum nächsten **etwas merkt**, greifst du zu einem Prozess. Prozesse sind der Ort, an dem die „sentencias secuenciales“ der HDL-Einheit leben, und sie sind die Brücke von der kombinatorischen Logik (Einheit III) zu den Speicherelementen (Einheit IV).

## Was ein Prozess ist

Ein `process` ist ein Block sequentiellen Codes innerhalb einer VHDL-Architektur. Seine Zeilen werden von oben nach unten gelesen, genau wie Software. Aber der Prozess selbst „läuft“ nur, wenn sich etwas in seiner **Sensitivitätsliste** ändert — die Liste ist die Aufwach-Bedingung des Prozesses.

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= '0';
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

Dieser Block bedeutet: „wann immer `clk` oder `reset` sich ändert, werte meinen Body neu aus.“ Zwischen diesen Events schläft der Prozess und `q` hält seinen Wert — was genau das Verhalten eines Registers ist.

## Zwei Formen, die du in 99 % der Fälle siehst

### Kombinatorischer Prozess

Sensitivitätsliste = **jedes im Inneren gelesene Signal**. Kein Takt. Der Ausgang ist eine reine Funktion der Eingänge.

```vhdl
process(a, b, sel)
begin
  if sel = '0' then
    y <= a;
  else
    y <= b;
  end if;
end process;
```

Synthetisiert zu einem 2-zu-1-MUX. Kein Speicher — vorausgesetzt, jeder Ausgang wird auf jedem Pfad zugewiesen (mehr dazu im Latch-Fix-it).

### Getakteter (sequentieller) Prozess

Sensitivitätsliste = `(clk, reset)` für einen asynchronen Reset oder nur `(clk)` für einen synchronen Reset. Die Datenzuweisungen passieren nur innerhalb von `rising_edge(clk)` (oder `falling_edge`). Der Ausgang ist **registriert** — er ändert sich nur an Taktflanken.

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= (others => '0');
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

Das ist das klassische D-Flip-Flop-Muster, und es ist die Vorlage für im Grunde jedes Register, das du je schreiben wirst.

## `rising_edge` vs. das alte `clk'event`-Idiom

Du siehst zwei Arten, eine Taktflanke zu erkennen:

```vhdl
elsif rising_edge(clk) then        -- bevorzugt
elsif clk'event and clk = '1' then -- älter, für '0'/'1' äquivalent
```

`rising_edge(clk)` (aus `std_logic_1164`) ist die moderne, klarere Form. Sie ignoriert auch korrekt Flanken, die Metawerte beteiligen (wie `'X'` → `'1'`), während das rohe `clk'event and clk='1'` bei ihnen fehlzünden kann. Nimm `rising_edge`.

## Die Sensitivitätslisten-Falle

Vergisst du, ein Signal aufzulisten, das ein *kombinatorischer* Body liest, sind Simulation und Synthese uneinig:

- **Simulation**: der Prozess wertet nicht neu aus, wenn sich dieses Signal ändert. Das Waveform sieht falsch aus (Ausgänge wirken „eingefroren“).
- **Synthese**: das Tool liest den Body, sieht jedes Lesen und baut trotzdem die richtige Hardware — es ignoriert deine Liste.

Resultat: deine Testbench und dein Board verhalten sich unterschiedlich, was die schlimmste Art von Bug ist. (Getaktete Prozesse sind gegen genau diese Falle immun, weil sie nur je an der Taktflanke handeln, aber eine unvollständige Liste bei einem *kombinatorischen* Prozess ist eine klassische Ursache für Phantom-Fehler.) VHDL-2008 behebt es rundheraus mit `process(all)`:

```vhdl
process(all)  -- das Tool schließt jedes im Body gelesene Signal ein
begin
  ...
end process;
```

Kmilas Parser akzeptiert `process(all)`, und das ist, was die meisten Teams für kombinatorische Prozesse in neuen Designs nutzen.

## Eine Faustregel

- Sensitivität = nur `(clk)` oder `(clk, reset)` → du beschreibst **Register** (Flip-Flops).
- Sensitivität = jeder Eingang, den du liest (oder `all`) → du beschreibst **kombinatorische Logik**.
- Sensitivität mischt eine Taktflanke mit zusätzlichen Datensignalen, die du dann *außerhalb* der Flanke nutzt → du hast fast sicher einen Bug.

## Noch eins: treib nicht dasselbe Signal aus zwei Prozessen

Ein Signal, das in einem Prozess getrieben wird, hat diesen Prozess als Treiber. Wenn ein *zweiter* Prozess (oder eine nebenläufige Anweisung) es auch treibt, hast du mehrere Treiber erzeugt — aufgelöst zu `'X'` für `std_logic` oder ein Compile-Fehler für nicht aufgelöste Typen. Gib jedem Signal genau einen Treiber.

## Was du behalten solltest

- Ein Prozess ist die Einheit sequentiellen Codes in einer ansonsten nebenläufigen Datei.
- Getaktete Prozesse (`rising_edge(clk)`) synthetisieren zu Flip-Flops — so bekommst du Speicher.
- Bevorzuge `rising_edge(clk)` gegenüber `clk'event and clk='1'`.
- Liste jedes Signal auf, das ein kombinatorischer Prozess liest, oder nutze `process(all)`; eine unvollständige Liste lässt Sim und Synthese uneinig sein.
- Ein Signal, ein Treiber.
