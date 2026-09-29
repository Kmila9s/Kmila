# Prozesse und Sensitivitätslisten

Alles in VHDL außerhalb eines Prozesses ist nebenläufig — Drähte und Gatter. Sobald du willst, dass eine Schaltung von einem Taktzyklus zum nächsten **etwas merkt**, brauchst du einen Prozess.

## Was ein Prozess ist

Ein `process` ist ein Block sequentiellen Codes innerhalb einer VHDL-Architektur. Seine Zeilen werden von oben nach unten gelesen, wie Software. Aber der Prozess selbst „läuft“ nur, wenn sich etwas in seiner **Sensitivitätsliste** ändert.

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

Dieser Block bedeutet: „wann immer `clk` oder `reset` sich ändert, werte meinen Body neu aus.“

## Zwei Formen, die du in 99 % der Fälle siehst

### Kombinatorischer Prozess

Sensitivitätsliste = jeder im Inneren gelesene Eingang. Kein Takt. Der Ausgang ist eine reine Funktion der Eingänge.

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

Synthetisiert zu einem 2-zu-1-MUX. Kein Speicher.

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

Das ist das klassische D-Flip-Flop-Muster.

## Die Sensitivitätslisten-Falle

Vergisst du, ein Signal aufzulisten, das der Body liest, sind Simulation und Synthese uneinig:

- **Simulation**: der Prozess wertet nicht neu aus, wenn sich dieses Signal ändert. Wellenform sieht falsch aus.
- **Synthese**: das Tool liest den Body, sieht jedes Lese und baut die richtige Hardware trotzdem.

Resultat: deine Testbench besteht, aber das Board geht nicht — oder umgekehrt. Modernes VHDL-2008 erlaubt `process(all)`, um das ganz zu vermeiden:

```vhdl
process(all)  -- jedes im Body gelesene Signal einschließen
begin
  ...
end process;
```

Kmilas Parser akzeptiert `process(all)`, und das ist, was die meisten Teams in neuen Designs nutzen.

## Faustregel

- Sensitivität = nur `(clk, reset)` → du beschreibst **Register** (Flip-Flops).
- Sensitivität = jeder gelesene Eingang → du beschreibst **kombinatorische Logik**.
- Sensitivität mischt Taktsignal + nicht-Taktsignal → du hast fast sicher einen Bug.

## Was du behalten solltest

- Ein Prozess ist die Einheit sequentiellen Codes in einer sonst nebenläufigen Datei.
- Getaktete Prozesse (`rising_edge(clk)`) synthetisieren zu Flip-Flops. So bekommst du Speicher.
- Liste jedes gelesene Signal auf, oder nutze `process(all)` und lass das Tool es für dich tun.
