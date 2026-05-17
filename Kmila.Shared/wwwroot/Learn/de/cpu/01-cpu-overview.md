# Wie eine CPU eine Instruktion ausführt

Schau in eine beliebige CPU — von einem kleinen 8-Bit-Mikrocontroller bis zu einem modernen GPU-Shader-Kern — und du wirst dieselben fünf Stufen finden, die dieselben fünf Aufgaben erledigen:

```mermaid
flowchart LR
    IF["IF<br/>fetch"] --> ID["ID<br/>decode"] --> EX["EX<br/>execute"] --> MEM["MEM<br/>Speicher"] --> WB["WB<br/>writeback"]
```

## Die fünf klassischen Stufen

**IF (Instruction Fetch).** Lies die nächste Instruktion aus dem Programmspeicher an der Adresse im Program Counter (PC). Inkrementiere den PC, damit wir im nächsten Zyklus auf die folgende Instruktion zeigen.

**ID (Decode).** Öffne das Instruktionswort. Zerlege es in einen **Opcode** (welche Operation?) und **Operandenfelder** (welche Register? welchen Immediate-Wert?).

**EX (Execute).** Speise die Operanden in die ALU. Für eine `ADD R0, R1`-Instruktion liest EX R0 und R1 aus der Registerdatei, sagt der ALU "addieren" und speichert das Ergebnis zwischen. Für eine `JUMP addr`-Instruktion berechnet EX den neuen PC.

**MEM (Memory).** Wenn die Instruktion ein Load oder Store ist, ist das die Stelle, an der du mit dem Datenspeicher sprichst. Für Register-zu-Register-Instruktionen ist die Stufe ein Durchreichen — aber die Stufe ist immer da, damit jede Instruktion dieselbe Pipeline-Tiefe hat.

**WB (Writeback).** Das Ergebnis landet schließlich an seinem Ziel — ein Register für Arithmetik, der PC für einen Sprung, eine Speicherzelle für einen Store.

## Single-Cycle vs. Multi-Cycle vs. Pipelined

Dieselben fünf Aufgaben können im Silizium auf drei verschiedene Arten angeordnet werden:

**Single-Cycle.** Alle fünf Stufen werden kombinatorisch innerhalb einer Taktperiode ausgeführt. Vorteile: einfach. Nachteile: Der Takt muss langsam genug sein, damit sich die längste Instruktion (meist ein Load) in einem Tick durch *alles* ausbreitet. Jedes einfache MIPS-Lehrbeispiel ist Single-Cycle.

**Multi-Cycle.** Eine Stufe pro Taktzyklus. Die Instruktion sitzt in einem Zustandsregister, während sie durch die Stufen wandert. Vorteile: schnellerer Takt als Single-Cycle (jede Stufe hat ihren eigenen kurzen kritischen Pfad). Nachteile: fünf Zyklen pro Instruktion.

**Pipelined.** Fünf Instruktionen gleichzeitig in Bearbeitung, jede in einer anderen Stufe. Vorteile: Eine neue Instruktion wird pro Takt fertig, der Durchsatz ist also ≈ Single-Cycle bei viel höherem Takt. Nachteile: Hazards (Daten, Control, Struktur), denen der Rest einer Vorlesung über Computerarchitektur gewidmet ist. RISC-V-Kerne, ARM Cortex-M, AMD Zen — alle pipelined.

## Das CPU-Projekt in diesem Lehrplan

Deine erste CPU wird **Single-Cycle** sein, um sie handhabbar zu halten. Alles passiert kombinatorisch innerhalb einer Taktperiode:

- PC adressiert ein 16-Byte-ROM.
- Das gelesene Byte ist sowohl das Opcode-Nibble als auch das Immediate-Nibble.
- Decode + Execute + Writeback geschehen alle vor der nächsten steigenden Taktflanke, die den neuen PC und das neue R0 erfasst.

## Die ISA, die wir implementieren

Vier Instruktionen, 4-Bit-Opcode + 4-Bit-Immediate:

| Opcode | Mnemonik      | Semantik                   |
|--------|---------------|----------------------------|
| `0000` | `LOAD R0, #i` | `R0 <= zero_extend(i)`     |
| `0001` | `ADD  R0, #i` | `R0 <= R0 + zero_extend(i)`|
| `0010` | `JUMP addr`   | `PC <= addr`               |
| `1111` | `HALT`        | friert PC und R0 ein       |

Ein Programm, das du schon von Hand ausführen kannst:

```
0: 03   LOAD R0, #3       -- R0 = 3
1: 14   ADD  R0, #4       -- R0 = 7
2: 12   ADD  R0, #2       -- R0 = 9
3: F0   HALT
```

## Was dir das bringt

Sobald das Gerüst läuft, hast du eine **programmierbare Maschine**. Ändere die Bytes im ROM — ändere das Programm, das die CPU ausführt. Das ist der fundamentale Trick: dieselbe Hardware, andere Inhalte im Speicher, anderes Verhalten.

Von hier aus kannst du wachsen:

- Füge mehr Register hinzu (R0..R3).
- Füge bedingte Sprünge hinzu (`JZ`, `JNZ`), damit deine Programme verzweigen können.
- Füge Loads/Stores aus einem RAM-Modul hinzu (verwende das aus dem Memories-Modul wieder).
- Pipeline das Ganze.

Der Weg von dieser 4-Instruktionen-Maschine zu einem vollen RV32I-RISC-V-Kern ist lang, aber jeder Schritt ist nur mehr von demselben Muster, das du gleich bauen wirst.
