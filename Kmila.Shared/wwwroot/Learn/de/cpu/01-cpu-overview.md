# Wie eine CPU einen Befehl ausführt

Schau in jede CPU — vom winzigen 8-Bit-Mikrocontroller bis zum modernen GPU-Shader-Kern — und du findest dieselben fünf Stufen, die dieselben fünf Aufgaben erledigen:

```mermaid
flowchart LR
    IF["IF<br/>Fetch"] --> ID["ID<br/>Decode"] --> EX["EX<br/>Execute"] --> MEM["MEM<br/>Memory"] --> WB["WB<br/>Writeback"]
```

## Die fünf klassischen Stufen

**IF (Instruction Fetch).** Lies den nächsten Befehl aus dem Programmspeicher an der Adresse, die im Program Counter (PC) steht. Inkrementiere den PC, damit wir im nächsten Zyklus auf den darauffolgenden Befehl zeigen.

**ID (Decode).** Zerlege das Befehlswort. Trenne es in einen **Opcode** (welche Operation?) und **Operanden-Felder** (welche Register? welcher Immediate?).

**EX (Execute).** Schiebe die Operanden in die ALU. Bei `ADD R0, R1` liest EX R0 und R1 aus dem Registerfile, sagt der ALU „add“ und latcht das Ergebnis. Bei `JUMP addr` berechnet EX den neuen PC.

**MEM (Memory).** Wenn der Befehl ein Load oder Store ist, sprichst du hier mit dem Datenspeicher. Bei Register-zu-Register-Befehlen ist die Stufe ein Durchgang — sie existiert aber immer, damit jeder Befehl dieselbe Pipeline-Tiefe hat.

**WB (Writeback).** Das Ergebnis landet endlich an seinem Ziel — ein Register bei Arithmetik, der PC bei einem Sprung, eine Speicherzelle bei einem Store.

## Single-Cycle vs. Multi-Cycle vs. Pipelined

Die gleichen fünf Aufgaben lassen sich auf drei verschiedene Weisen in Silizium gießen:

**Single-Cycle.** Alle fünf Stufen laufen kombinatorisch innerhalb einer Taktperiode. Pro: einfach. Contra: Der Takt muss langsam genug sein, damit der längste Befehl (meist ein Load) in einem Tick durch *alles* propagiert. Jedes einfache MIPS-Lehrbuchbeispiel ist Single-Cycle.

**Multi-Cycle.** Eine Stufe pro Takt. Der Befehl wartet in einem Zustandsregister, während er durch die Stufen wandert. Pro: schnellerer Takt als Single-Cycle (jede Stufe hat einen kürzeren kritischen Pfad). Contra: fünf Zyklen pro Befehl.

**Pipelined.** Fünf Befehle gleichzeitig unterwegs, jeder in einer anderen Stufe. Pro: ein neuer Befehl tritt jeden Takt aus, also Durchsatz ≈ Single-Cycle bei viel schnellerem Takt. Contra: Hazards (Daten, Control, strukturell), denen der Rest einer Vorlesung über Rechnerarchitektur gewidmet ist. RISC-V-Kerne, ARM Cortex-M, AMD Zen — alle pipelined.

## Das CPU-Projekt in diesem Curriculum

Deine erste CPU wird **Single-Cycle**, um sie handhabbar zu halten. Alles passiert kombinatorisch innerhalb einer Taktperiode:

- PC adressiert ein 16-Byte-ROM.
- Das geholte Byte ist gleichzeitig das Opcode-Nibble und das Immediate-Nibble.
- Decode + Execute + Writeback passieren alle vor der nächsten steigenden Taktflanke, die den neuen PC und das neue R0 latcht.

## Die ISA, die wir implementieren

Vier Befehle, 4-Bit-Opcode + 4-Bit-Immediate:

| Opcode | Mnemonic      | Semantik                   |
|--------|---------------|----------------------------|
| `0000` | `LOAD R0, #i` | `R0 <= zero_extend(i)`     |
| `0001` | `ADD  R0, #i` | `R0 <= R0 + zero_extend(i)`|
| `0010` | `JUMP addr`   | `PC <= addr`               |
| `1111` | `HALT`        | friert PC und R0 ein       |

Ein Programm, das du schon von Hand laufen lassen kannst:

```
0: 03   LOAD R0, #3       -- R0 = 3
1: 14   ADD  R0, #4       -- R0 = 7
2: 12   ADD  R0, #2       -- R0 = 9
3: F0   HALT
```

## Was dir das bringt

Sobald das Gerüst läuft, hast du eine **programmierbare Maschine**. Ändere die Bytes im ROM — ändere das Programm, das die CPU ausführt. Das ist der Grundtrick: dieselbe Hardware, anderer Speicher­inhalt, anderes Verhalten.

Von hier aus kannst du wachsen:

- Mehr Register (R0..R3) hinzufügen.
- Bedingte Sprünge (`JZ`, `JNZ`) hinzufügen, damit deine Programme verzweigen können.
- Loads/Stores aus einem RAM-Modul (verwende das aus dem Memories-Modul wieder).
- Das Ganze pipelinen.

Der Weg von dieser 4-Befehl-Maschine zu einem vollständigen RV32I-RISC-V-Kern ist lang, aber jeder Schritt ist nur mehr vom selben Muster, das du gleich bauen wirst.
