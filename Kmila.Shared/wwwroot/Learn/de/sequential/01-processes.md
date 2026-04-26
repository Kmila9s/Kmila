# Prozesse und Sensitivitätslisten

Alles in VHDL außerhalb eines Prozesses ist nebenläufig — Drähte und Gatter. In dem Moment, in dem du möchtest, dass sich eine Schaltung etwas von einem Taktzyklus zum nächsten **merkt**, brauchst du einen Prozess.

## Was ein Prozess ist

Ein `process` ist ein Block sequentiellen Codes innerhalb einer VHDL-Architektur. Seine Zeilen werden von oben nach unten gelesen, genau wie Software. Aber der Prozess selbst "läuft" nur, wenn sich etwas in seiner **Sensitivitätsliste** ändert.

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

Dieser Block bedeutet: "immer wenn `clk` oder `reset` sich ändert, werte meinen Rumpf neu aus."

## Zwei Formen, die du in 99 % der Fälle siehst

### Kombinatorischer Prozess

Sensitivitätsliste = jeder Eingang, der im Rumpf gelesen wird. Kein Takt. Ausgang ist eine reine Funktion der Eingänge.

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

Wird zu einem 2-zu-1-Mux synthetisiert. Kein Speicher.

### Getakteter (sequentieller) Prozess

Sensitivitätsliste = `(clk, reset)`. Zuweisungen passieren nur innerhalb von `rising_edge(clk)` (oder `falling_edge`, wenn du Pech hast). Der Ausgang ist **registriert** — er ändert sich nur an Taktflanken.

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

Das ist das klassische D-Flipflop-Muster.

## Die Sensitivitätslisten-Falle

Wenn du vergisst, ein Signal aufzulisten, das der Rumpf liest, weichen Simulation und Synthese voneinander ab:

- **Simulation**: Der Prozess wird nicht neu ausgewertet, wenn sich dieses Signal ändert. Waveform sieht falsch aus.
- **Synthese**: Das Tool liest den Rumpf, sieht jeden Lesezugriff und baut sowieso die richtige Hardware.

Ergebnis: Dein Testbench besteht, aber das Board funktioniert nicht — oder umgekehrt. Modernes VHDL-2008 erlaubt dir `process(all)`, um das ganz zu vermeiden:

```vhdl
process(all)  -- schließt jedes im Rumpf gelesene Signal ein
begin
  ...
end process;
```

Kmilas Parser akzeptiert `process(all)`, und das ist, was die meisten Teams in neuen Designs verwenden.

## Eine Faustregel

- Sensitivität = nur `(clk, reset)` → du beschreibst **Register** (Flipflops).
- Sensitivität = jeden Eingang, den du liest → du beschreibst **kombinatorische Logik**.
- Sensitivität mischt getaktete und nicht-getaktete Signale → du hast ziemlich sicher einen Bug.

## Was zu merken ist

- Ein Prozess ist die Einheit sequentiellen Codes innerhalb einer ansonsten nebenläufigen Datei.
- Getaktete Prozesse (`rising_edge(clk)`) werden zu Flipflops synthetisiert. So bekommst du Speicher.
- Liste jedes Signal auf, das du liest, oder verwende `process(all)`, damit das Tool es für dich tut.
