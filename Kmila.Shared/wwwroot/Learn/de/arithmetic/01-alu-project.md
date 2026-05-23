# Projekt: 8-Bit-ALU mit Flags

Eine **arithmetisch-logische Einheit** (ALU) ist das schlagende Herz jeder CPU. Gib ihr zwei Operanden und einen Opcode; sie liefert ein Ergebnis und eine Handvoll Flags, die beschreiben, was passiert ist (Überlauf? Ergebnis null? usw.).

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

- `zero` — high, wenn `y` komplett null ist
- `carry` — der Carry-out aus ADD/SUB; bei allem anderen `'0'` treiben

## Was das Gerüst dir gibt

Der Starter deklariert die Entity, die Signale `result` und `cout` und eine `process(op, a, b)`-Hülle, die ADD und SUB schon abhandelt. Dein Job: die anderen sechs Zweige des `case`-Statements auffüllen.

## Tipps

- Für **AND / OR / XOR** je ein Einzeiler — einfach das bitweise Ergebnis zuweisen:

  ```vhdl
  when "010" => result <= a and b;
  ```

- Für **SHL** und **SHR** mit `'0'` auffüllen:

  ```vhdl
  when "101" => result <= a(6 downto 0) & '0';
  when "110" => result <= '0' & a(7 downto 1);
  ```

- Für **PASS_A** einfach `a` in `result` kopieren.

Vergiss nicht, `cout <= '0'` bei Logik-Ops zu treiben — du machst das einmal oben im Prozess, was heißt: jeder Zweig, der es nicht überschreibt, erbt den Default. Schön, bedeutet aber, dass du explizit sein musst, wenn ein Zweig **doch** einen Nicht-Null-Carry will.

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

- Füge ein **signed**-Flag hinzu (negatives Ergebnis bei Zweier-Komplement-SUB).
- Füge einen **rotate**-Opcode hinzu — wie SHL, aber das oberste Bit kommt unten wieder rein.
- Füge einen **multiply**-Opcode hinzu, der ein 16-Bit-Ergebnis auf zwei getrennten Ausgangsports liefert. Die meisten kleinen FPGAs haben dafür dedizierte DSP-Blöcke — beobachte die Auslastung beim Resynth.

Diese ALU ist ein direkter Baustein für das CPU-Projekt auf Expert-Niveau — halte den Entwurf sauber, du wirst ihn dort verdrahten.
