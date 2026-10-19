# Projekt: 8-Bit-ALU mit Flags

Eine **arithmetisch-logische Einheit** (ALU) ist das schlagende Herz jeder CPU. Gib ihr zwei Operanden und einen Opcode; sie liefert dir ein Ergebnis und eine Handvoll Flags, die beschreiben, was passiert ist (gab es einen Überlauf? ist das Ergebnis null? usw.).

## Was du baust

Eine 8-Bit-ALU mit folgenden Operationen:

| Opcode | Name    | Formel                        |
|--------|---------|-------------------------------|
| `000`  | ADD     | `y = a + b`                   |
| `001`  | SUB     | `y = a - b`                   |
| `010`  | AND     | `y = a and b`                 |
| `011`  | OR      | `y = a or  b`                 |
| `100`  | XOR     | `y = a xor b`                 |
| `101`  | SHL     | `y = a um 1 nach links`       |
| `110`  | SHR     | `y = a um 1 nach rechts`      |
| `111`  | PASS_A  | `y = a` (Durchreichen)        |

Plus zwei Flag-Ausgänge:

- `zero` – high, wenn `y` komplett null ist
- `carry` – der Carry-out aus ADD/SUB; bei allem anderen `'0'` treiben

Im ESCOM-Kurs *Arquitectura de Computadoras* ist das Thema 3.1, der erste funktionale Block des Single-Cycle-RISC-Prozessors. Bau ihn jetzt sauber, und du steckst ihn später direkt in den Datenpfad.

## Der Addierer darunter

ADD und SUB sehen wie eine Zeile VHDL aus (`a + b`), aber die Hardware, die die Synthese für dieses `+` baut, ist der Ort, an dem die interessante Ingenieursarbeit steckt. Der Kurs verlangt, zwei klassische Strukturen zu vergleichen.

**Ripple-Carry-Addierer.** Verkette acht Volladdierer und speise den Carry-out jeder Stufe in den Carry-in der nächsten. Winzig und offensichtlich, aber der Übertrag muss von Bit 0 bis ganz hoch zu Bit 7 *durchrippeln*, bevor das oberste Summenbit gültig ist – die Worst-Case-Verzögerung wächst also linear mit der Wortbreite. Für einen 8-Bit-Addierer sind das acht Volladdierer-Verzögerungen hintereinander.

**Carry-Lookahead-Addierer (CLA).** Statt auf das Rippeln zu warten, berechne jeden Übertrag direkt aus den Eingängen. Definiere zwei Signale pro Bit:

```
generate:  g(i) = a(i) and b(i)      -- dieses Bit erzeugt selbst einen Übertrag
propagate: p(i) = a(i) xor b(i)      -- dieses Bit reicht einen ankommenden Übertrag weiter
```

Dann ist jeder Übertrag `c(i+1) = g(i) or (p(i) and c(i))`, so ausgerollt, dass die Überträge parallel statt nacheinander erzeugt werden. Die Verzögerung wird mit der Breite etwa konstant, zum Preis von mehr Gattern. Das ist das „acarreo anticipado" (vorweggenommener Übertrag), das der Lehrplan dem kaskadierten „acarreo en cascada"-Ripple-Design gegenüberstellt.

Für dieses Projekt musst du keines von beiden von Hand bauen – ein `a + b` zu schreiben lässt die FPGA-Tools eine optimierte Carry-Kette wählen (die Artix-7-Fabric hat sogar dedizierte Fast-Carry-Logik). Der Punkt ist, zu *verstehen*, welchen Trade-off dieses `+` darstellt: Fläche gegen Geschwindigkeit.

## Die Flags richtig hinbekommen

Flags sind der Ort, an dem Anfänger still Punkte verlieren, denn **Übertrag** und **Überlauf** sind zwei verschiedene Dinge:

- **Übertrag (Carry, C)** ist das Bit, das oben aus einer *vorzeichenlosen* Addition herausfällt. Es ist das 9. Bit von `a + b`.
- **Überlauf (Overflow, V)** signalisiert, dass ein *vorzeichenbehaftetes* (Zweierkomplement-)Ergebnis nicht mehr in 8 Bit passt. Die saubere Regel: `V = carry_into_MSB xor carry_out_of_MSB`. Gleichbedeutend: Ein Überlauf ist passiert, wenn zwei vorzeichengleiche Operanden ein vorzeichenentgegengesetztes Ergebnis erzeugt haben.

Ein sauberer VHDL-Trick für beide auf einmal ist, in einem 9-Bit-Raum zu addieren:

```vhdl
signal ext : unsigned(8 downto 0);
...
ext   <= ('0' & unsigned(a)) + ('0' & unsigned(b));  -- 9-Bit-Addition
result <= std_logic_vector(ext(7 downto 0));
cout   <= ext(8);                                     -- das Carry-Flag
```

Für SUB: Denk daran, dass Subtraktion im Zweierkomplement `a + (not b) + 1` ist, also erzeugt `15 - 15` intern einen Carry-out von `1`, obwohl das Ergebnis null ist – genau deshalb behandeln viele ISAs den Subtraktions-Übertrag als *Borrow* (C = not borrow). Halte deine Konvention konsistent mit dem, was die begleitende Testbench erwartet.

## Was das Gerüst dir gibt

Der Starter deklariert die Entity, die Signale `result` und `cout` und eine `process(op, a, b)`-Hülle, die ADD und SUB schon abhandelt. Dein Job: die anderen sechs Zweige des `case`-Statements auffüllen.

## Tipps

- Für **AND / OR / XOR** je ein Einzeiler – einfach das bitweise Ergebnis zuweisen:

  ```vhdl
  when "010" => result <= a and b;
  ```

- Für **SHL** und **SHR** mit `'0'` auffüllen:

  ```vhdl
  when "101" => result <= a(6 downto 0) & '0';
  when "110" => result <= '0' & a(7 downto 1);
  ```

- Für **PASS_A** einfach `a` in `result` kopieren.

Vergiss nicht, `cout <= '0'` bei Logik-Ops zu treiben – du machst das bereits einmal oben im Prozess, was heißt: Jeder Zweig, der es nicht überschreibt, erbt diesen Default. Praktisch, bedeutet aber, dass du explizit sein musst, wenn ein Zweig **doch** einen Nicht-Null-Übertrag will.

## Akzeptanzkriterien

Mit den Stimuli aus der Begleit-Config:

| Zeit | op    | a          | b          | erwartet `y` | `carry` | `zero` |
|------|-------|------------|------------|--------------|---------|--------|
|  0   | `000` | `00001111` | `00000101` | `00010100`   | `0`     | `0`    |
| 20   | `001` | `00001111` | `00001111` | `00000000`   | `0`     | `1`    |
| 40   | `010` | `00001111` | `00001111` | `00001111`   | `0`     | `0`    |
| 60   | `101` | `10110000` | n/a        | `01100000`   | `0`     | `0`    |
| 80   | `111` | `10110000` | n/a        | `10110000`   | `0`     | `0`    |

## Weitergehen

- Füge ein **signed**-Flag hinzu (negatives Ergebnis bei Zweierkomplement-SUB).
- Füge einen **rotate**-Opcode hinzu – wie SHL, aber das oberste Bit kommt unten bei Bit 0 wieder rein.
- Füge einen **multiply**-Opcode hinzu, der ein 16-Bit-Ergebnis auf zwei getrennten Ausgangsports liefert. Die meisten kleinen FPGAs haben dafür dedizierte DSP-Blöcke – beobachte deine Auslastung beim erneuten Synthetisieren.

Diese ALU ist ein direkter Baustein für das CPU-Projekt auf Expert-Niveau – halte diesen Entwurf sauber, du wirst ihn dort verdrahten.

## Was du behalten solltest

- Eine ALU ist Operanden rein, Ergebnis-und-Flags raus. Der Opcode wählt, welche Operation der eine Block ausführt.
- Das `+`, das du schreibst, verbirgt eine echte Wahl: Ripple-Carry (klein, langsam, Verzögerung wächst mit der Breite) gegen Carry-Lookahead (größer, schnell, Verzögerung etwa konstant) mittels Generate/Propagate-Termen.
- **Übertrag ≠ Überlauf.** Der Übertrag ist das vorzeichenlose 9. Bit; der Überlauf ist `carry_in(MSB) xor carry_out(MSB)` und zählt nur bei vorzeichenbehafteten Werten.
- Setz für jedes Flag oben im Prozess einen sinnvollen Default und überschreib nur dort, wo ein Zweig es wirklich braucht – so vermeidest du Latches und vergessene Fälle.
