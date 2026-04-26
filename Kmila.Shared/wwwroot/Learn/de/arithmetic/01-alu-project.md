# Projekt: 8-Bit-ALU mit Flags

Eine **Arithmetic Logic Unit (ALU)** ist das schlagende Herz jeder CPU. Du gibst ihr zwei Operanden und einen Opcode; sie gibt dir ein Ergebnis zurück und eine Handvoll Flags, die beschreiben, was passiert ist (gab es einen Überlauf? ist das Ergebnis null? usw.).

## Was du baust

Eine 8-Bit-ALU mit folgenden Operationen:

| Opcode | Name    | Formel                      |
|--------|---------|-----------------------------|
| `000`  | ADD     | `y = a + b`                 |
| `001`  | SUB     | `y = a - b`                 |
| `010`  | AND     | `y = a and b`               |
| `011`  | OR      | `y = a or  b`               |
| `100`  | XOR     | `y = a xor b`               |
| `101`  | SHL     | `y = a um 1 nach links`     |
| `110`  | SHR     | `y = a um 1 nach rechts`    |
| `111`  | PASS_A  | `y = a` (Durchreichen)      |

Plus zwei Flag-Ausgänge:

- `zero` — hoch, wenn `y` komplett aus Nullen besteht
- `carry` — der Carry-Out von ADD/SUB; treibe `'0'` für alles andere

## Was dir das Gerüst gibt

Der Starter deklariert die Entity, die Signale `result` und `cout` sowie eine `process(op, a, b)`-Hülle, die bereits ADD und SUB behandelt. Deine Aufgabe ist es, die anderen sechs Zweige der `case`-Anweisung zu füllen.

## Hinweise

- Für **AND / OR / XOR** ist es jeweils ein Einzeiler — weise einfach das bitweise Ergebnis zu:

  ```vhdl
  when "010" => result <= a and b;
  ```

- Für **SHL** und **SHR** fülle mit `'0'` auf:

  ```vhdl
  when "101" => result <= a(6 downto 0) & '0';
  when "110" => result <= '0' & a(7 downto 1);
  ```

- Für **PASS_A** kopiere einfach `a` nach `result`.

Denk daran, `cout <= '0'` für Logikoperationen zu treiben — du tust das bereits einmal am Anfang des Prozesses, was bedeutet, dass jeder Zweig, der es nicht überschreibt, diesen Default erbt. Schön, aber es bedeutet auch, dass du explizit sein musst, wenn ein Zweig **doch** einen Carry ungleich null haben soll.

## Akzeptanzkriterien

Mit den Stimuli in der zugehörigen Konfiguration:

| Zeit | op    | a          | b          | erwartetes `y` | `carry` | `zero` |
|------|-------|------------|------------|----------------|---------|--------|
|  0   | `000` | `00001111` | `00000101` | `00010100`     | `0`     | `0`    |
| 20   | `001` | `00001111` | `00001111` | `00000000`     | `0`     | `1`    |
| 40   | `010` | `00001111` | `00001111` | `00001111`     | `0`     | `0`    |
| 60   | `101` | `10110000` | n/a        | `01100000`     | `0`     | `0`    |
| 80   | `111` | `10110000` | n/a        | `10110000`     | `0`     | `0`    |

## Noch weiter

- Füge ein **signed**-Flag hinzu (negatives Ergebnis bei SUB im Zweierkomplement).
- Füge einen **rotate**-Opcode hinzu — wie SHL, aber das oberste Bit wandert zurück zu Bit 0.
- Füge einen **multiply**-Opcode hinzu, der ein 16-Bit-Ergebnis auf zwei separaten Ausgangsports erzeugt. Die meisten kleinen FPGAs haben dafür dedizierte DSP-Blöcke — achte auf deine Auslastung beim Neu-Synthetisieren.

Diese ALU ist ein direkter Baustein für das CPU-Projekt auf Expert-Level — halte dieses Design sauber, du wirst es dort verdrahten.
