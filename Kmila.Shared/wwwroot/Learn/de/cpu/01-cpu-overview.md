# Wie eine CPU einen Befehl ausführt

Schau in jede CPU – vom winzigen 8-Bit-Mikrocontroller bis zum modernen GPU-Shader-Kern – und du findest dieselben fünf Stufen, die dieselben fünf Aufgaben erledigen:

```mermaid
flowchart LR
    IF["IF<br/>fetch"] --> ID["ID<br/>decode"] --> EX["EX<br/>execute"] --> MEM["MEM<br/>memory"] --> WB["WB<br/>writeback"]
```

## Die fünf klassischen Stufen

**IF (Instruction Fetch).** Lies den nächsten Befehl aus dem Programmspeicher an der Adresse, die im Program Counter (PC) steht. Inkrementiere den PC, damit wir im nächsten Zyklus auf den darauffolgenden Befehl zeigen.

**ID (Decode).** Zerlege das Befehlswort. Trenne es in einen **Opcode** (welche Operation?) und **Operanden-Felder** (welche Register? welcher Immediate?).

**EX (Execute).** Schiebe die Operanden in die ALU. Bei `ADD R0, R1` liest EX R0 und R1 aus dem Registersatz, sagt der ALU „add" und latcht das Ergebnis. Bei `JUMP addr` berechnet EX den neuen PC.

**MEM (Memory).** Wenn der Befehl ein Load oder Store ist, sprichst du hier mit dem Datenspeicher. Bei Register-zu-Register-Befehlen ist die Stufe ein Durchgang – sie existiert aber immer, damit jeder Befehl dieselbe Pipeline-Tiefe hat.

**WB (Writeback).** Das Ergebnis landet endlich an seinem Ziel – ein Register bei Arithmetik, der PC bei einem Sprung, eine Speicherzelle bei einem Store.

## Single-Cycle vs. Multi-Cycle vs. Pipelined

Die gleichen fünf Aufgaben lassen sich auf drei verschiedene Weisen in Silizium gießen:

**Single-Cycle.** Alle fünf Stufen laufen kombinatorisch innerhalb einer Taktperiode. Pro: einfach. Contra: Der Takt muss langsam genug sein, damit der längste Befehl (meist ein Load) in einem Tick durch *alles* propagiert. Jedes einfache MIPS-Lehrbuchbeispiel ist Single-Cycle.

**Multi-Cycle.** Eine Stufe pro Takt. Der Befehl wartet in einem Zustandsregister, während er durch die Stufen wandert. Pro: schnellerer Takt als Single-Cycle (jede Stufe hat einen kürzeren kritischen Pfad). Contra: fünf Zyklen pro Befehl.

**Pipelined.** Fünf Befehle gleichzeitig unterwegs, jeder in einer anderen Stufe. Pro: Jeden Takt tritt ein neuer Befehl aus, also Durchsatz ≈ Single-Cycle bei viel schnellerem Takt. Contra: Hazards (Daten, Control, strukturell), denen der Rest einer Vorlesung über Rechnerarchitektur gewidmet ist. RISC-V-Kerne, ARM Cortex-M, AMD Zen – alle pipelined.

## RISC, CISC und warum die Stufen so sauber sind

Diese fünf ordentlichen Stufen sind kein Zufall – sie sind das, was du bekommst, wenn der Befehlssatz auf Regelmäßigkeit ausgelegt ist. Das ist die **RISC**-Philosophie (Reduced Instruction Set Computer), um die der ESCOM-Kurs herum aufgebaut ist:

- **Befehle fester Breite** (jeder Befehl hat gleich viele Bits), sodass Fetch und Decode trivial sind.
- **Load/Store-Architektur** – nur dedizierte Load- und Store-Befehle fassen den Speicher an; Arithmetik läuft Register zu Register. Deshalb ist MEM eine saubere, separate Stufe.
- **Viele Allzweck-Register**, sodass Compiler Werte im Registersatz halten, statt sie in den Speicher auszulagern.

**CISC** (Complex Instruction Set Computer, z. B. x86) setzt auf das Gegenteil: Befehle variabler Länge, Operationen, die in einem Befehl Speicher lesen *und* rechnen, und hunderte Adressierungsarten. Mächtig pro Befehl, aber viel schwerer sauber zu pipelinen. Moderne x86-Chips zerlegen jeden komplexen Befehl intern tatsächlich in RISC-artige Mikro-Ops.

Noch eine Trennung, die es zu benennen lohnt: **Von-Neumann**-Maschinen halten Programm und Daten in einem Speicher (ein Bus, einfach, aber Code und Daten konkurrieren darum); **Harvard**-Maschinen nutzen getrennte Befehls- und Datenspeicher, was genau das ist, was einem Single-Cycle- oder Pipeline-Kern erlaubt, im selben Tick einen Befehl zu holen *und* auf Daten zuzugreifen. Die CPU, die du gleich baust, ist faktisch Harvard – ein ROM für den Code, ein separater Pfad für die Daten.

## Befehlsformate

Decode ist nur deshalb einfach, weil die Felder jedes Befehls an festen Positionen sitzen. Klassisches RISC (MIPS-Stil, das Modell, das ESCOM verwendet) hat drei Formate:

- **R-Typ** – Register-zu-Register-ALU-Operationen: `opcode | rs | rt | rd | shamt | funct`. Zwei Quellregister, ein Ziel, ein Schiebebetrag und ein Funktionsselektor.
- **I-Typ** – Immediate / Load / Store / Branch: `opcode | rs | rt | immediate`. Der 16-Bit-Immediate ist eine Konstante oder ein Adress-Offset.
- **J-Typ** – Sprünge: `opcode | address`. Ein großes Adressfeld, um weit entfernte Ziele zu erreichen.

Die Spielzeug-ISA weiter unten ist ein Zwei-Feld-Verwandter dieser Idee: ein 4-Bit-Opcode und ein 4-Bit-Immediate. Gleiches Prinzip, kleiner.

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

Sobald das Gerüst läuft, hast du eine **programmierbare Maschine**. Ändere die Bytes im ROM – ändere das Programm, das die CPU ausführt. Das ist der Grundtrick: dieselbe Hardware, anderer Speicherinhalt, anderes Verhalten.

Von hier aus kannst du wachsen:

- Mehr Register (R0..R3) hinzufügen.
- Bedingte Sprünge (`JZ`, `JNZ`) hinzufügen, damit deine Programme verzweigen können.
- Loads/Stores aus einem RAM-Modul (verwende das aus dem Memories-Modul wieder).
- Das Ganze pipelinen.

Der Weg von dieser 4-Befehl-Maschine zu einem vollständigen RV32I-RISC-V-Kern ist lang, aber jeder Schritt ist nur mehr vom selben Muster, das du gleich bauen wirst.

## Wer das alles steuert: die Steuereinheit

Der Datenpfad (PC, ROM, Registersatz, ALU, Speicher) ist die Verrohrung; die **Steuereinheit** (Control Unit) ist das, was jeden Zyklus die richtigen Ventile öffnet. Sie liest den Opcode und treibt die Steuersignale: welche ALU-Operation, ob der Registersatz in diesem Zyklus schreibt, ob der Speicher liest oder schreibt, woher der nächste PC kommt. Zwei Wege, sie zu bauen:

- **Festverdrahtete Steuerung** – der Opcode speist kombinatorische Logik (oder einen winzigen Zustandsautomaten), die die Signale direkt erzeugt. Schnell und die natürliche Wahl für ein sauberes RISC. Das ist das, was dein Gerüst mit seinem `case` auf dem Opcode tut.
- **Mikroprogrammierte Steuerung** – jeder Befehl ist ein kurzes Programm aus *Mikrobefehlen*, die in einem Mikrocode-ROM gespeichert sind und die Steuersignale ausschrittweise erzeugen. Flexibel und historisch der Weg, wie CISC-Maschinen ihre Komplexität bändigten, zum Preis von Geschwindigkeit. Der ESCOM-Lehrplan nennt beide (microinstrucciones, memorias de microcódigo), damit du sie auseinanderhalten kannst.

Daneben sitzt das **Statusregister** – die Flags, die deine ALU erzeugt hat (zero, carry, overflow, negative). Bedingte Sprünge sind nur die Steuereinheit, die eines dieser Flags testet, um den nächsten PC zu entscheiden.

## Was du behalten solltest

- Jede CPU, groß oder klein, läuft dieselbe Schleife: Fetch, Decode, Execute, Memory, Writeback.
- Diese Stufen können Single-Cycle (einfach, langsamer Takt), Multi-Cycle (schnellerer Takt, mehrere Zyklen je Befehl) oder pipelined (überlappt, höchster Durchsatz) sein.
- RISC hält Befehle fester Breite und Load/Store, damit Decode und Pipelining sauber bleiben; CISC gibt das für Mächtigkeit pro Befehl auf.
- Der Datenpfad bewegt die Daten; die Steuereinheit – festverdrahtet oder mikroprogrammiert – entscheidet, was wohin bewegt wird, anhand des Opcodes und der Status-Flags.
