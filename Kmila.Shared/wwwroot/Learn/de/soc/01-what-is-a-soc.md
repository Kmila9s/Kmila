# Was ist ein System-on-Chip?

Deine 8-Bit-CPU aus dem vorigen Track läuft eigenständig. Sie rechnet, aber sie spricht nicht mit der Welt. Ein **System-on-Chip** ist die nächste Größenordnung: eine CPU plus eine Handvoll Peripheriebausteine plus das Geflecht, das sie miteinander reden lässt – alles auf demselben Die.

Das ist die ganze Prämisse des Kurses *Sistemas en Chip* – statt einen Prozessor auf einer Platine mit separaten Speicherchips und separaten I/O-Chips zu verdrahten, integrierst du den Prozessor, seinen Speicher und seine Peripherie in ein einziges Device und programmierst das Ergebnis.

## Drei zwingende Bausteine

Jeder SoC, von der erweiterten Kmila-CPU bis zur 5-Millionen-Dollar-Apple-A-Serie, läuft auf Folgendes hinaus:

1. **Eine oder mehrere Recheneinheiten** – CPUs, DSPs, GPUs, Beschleuniger.
2. **Speicher** – On-Chip-SRAM, Block-RAM, manchmal externer DDR.
3. **Ein Interconnect** – die Drähte + Arbitrierung, die (1) mit (2) und mit der Peripherie reden lässt.

Das Interconnect ist der Teil, der aufhört, „nur RTL" zu sein, und architektonisch wird. Es regelt, wer wann reden darf, wie viele Transaktionen gleichzeitig unterwegs sein dürfen und wie schnell eine gegebene Anfrage abgeschlossen werden kann.

## Die Recheneinheit: RISC und Harvard

Die meisten SoC-Kerne, denen du begegnest – PIC, AVR, ARM Cortex-M, RISC-V – sind **RISC** (Reduced Instruction Set Computer). RISC bedeutet einen kleinen Satz einfacher Befehle fester Breite, die jeweils eine Sache tun, sodass die Hardware zum Decodieren und Ausführen klein und schnell bleibt. Der Tauschhandel: Komplexe Operationen brauchen mehrere Befehle statt eines.

Diese Kerne sind meist **Harvard**-Architektur: Programmspeicher und Datenspeicher sitzen auf *getrennten* Bussen. Weil die beiden Busse unabhängig sind, kann der Kern den nächsten Befehl holen, während er gleichzeitig einen Datenoperanden liest oder schreibt – doppelte Speicherbandbreite gegenüber einem einzelnen geteilten Bus. Netter Nebeneffekt: Das Befehlswort muss nicht der Datenwortbreite entsprechen. Ein PIC-Mid-Range-Kern läuft zum Beispiel mit 8-Bit-Daten, aber 14-Bit-Befehlen, sodass jeder Befehl in ein einziges Wort passt.

(Eine reine **Von-Neumann**-Maschine teilt einen Bus für Code und Daten – einfacher, aber Code- und Daten-Fetches konkurrieren darum. Der 8051 ist ein „modifizierter Harvard"-Kompromiss.)

## Pipelining

Ein Kern überlappt Arbeit, um mehr Befehle pro Sekunde fertigzustellen. Die einfachste Form ist eine zweistufige **Pipeline**: Während Befehl *N* ausführt, holt der Kern schon Befehl *N+1*.

```
  Zyklus:  1        2        3        4
  fetch:  I1       I2       I3       I4
  exec:            I1       I2       I3
```

Der Haken sind **Sprünge**. Wenn ein `GOTO`, `CALL` oder `RETURN` den Program Counter ändert, ist der schon dahinter geholte Befehl falsch und muss weggeworfen werden – die Pipeline *flusht* und füllt neu, was einen Zyklus kostet. Deshalb laufen enge Schleifen und sprungstarker Code etwas langsamer als geradliniger Code.

## Programmspeicher, Datenspeicher und das Programmiermodell

Die ESCOM-Einheit teilt die Speicherkarte in zwei Rollen:

- **Programmspeicher** hält deine Befehle (oft Flash/ROM). Nichtflüchtig, sodass das Device bei jedem Einschalten dasselbe Programm bootet.
- **Datenspeicher** hält Variablen, den Stack und I/O-Register (SRAM). Flüchtig, beim Reset gelöscht.

Darüber liegt das **Programmiermodell**: die Register und die Adresskarte, die der Programmierer tatsächlich sieht. Es definiert die Allzweck-Register, die Special-Function-Register (Status-Flags, den Program Counter, den Stack Pointer) und – entscheidend für einen SoC – die **memory-mapped Peripherie-Register**. In einem memory-mapped Design sieht eine Peripherie wie ein paar Adressen aus: Schreib ein Byte an eine Adresse, und der UART sendet es; lies eine andere Adresse, und du bekommst das ADC-Ergebnis. Die CPU lernt nie einen neuen Befehl für jede Peripherie; sie macht einfach Loads und Stores.

## Gängige Interconnect-Protokolle

Die Industrie hat sich für dieses Load/Store-Geflecht auf eine Handvoll Standards geeinigt:

| Protokoll        | Hersteller | Wo du es siehst                                   |
|------------------|------------|---------------------------------------------------|
| AXI4 / AXI4-Lite | ARM        | RISC-V-Kerne, Xilinx PS/PL-Grenze, fast alles Neue |
| AHB / APB        | ARM        | Ältere ARM-Kerne, langsame Peripherie             |
| Avalon MM / ST   | Intel      | NIOS-II, Qsys-generierte SoCs                      |
| Wishbone         | OpenCores  | OpenRISC, viele Open-Source-IP-Kerne              |

Alle laufen auf dieselben Spuren hinaus: einen **Adresskanal**, einen **Schreibdatenkanal** und einen **Lesedatenkanal**. Unterschiede gibt es beim Handshake-Protokoll, bei Out-of-Order-Unterstützung und bei der Burst-Semantik.

## Was AXI4-Lite eigentlich ist

Das simpelste Mitglied der AXI-Familie. Lass Bursts, Out-of-Order und QoS weg, und du bekommst fünf unabhängige Kanäle, jeder mit `valid`/`ready`-Handshake:

```
  AW  (address write)
  W   (write data)
  B   (write response)
  AR  (address read)
  R   (read data)
```

Jeder Transfer ist ein Beat. Jeder Kanal hält an, wenn die Gegenseite nicht `ready` ist. Mehr nicht – was genau der Grund ist, warum es die richtige Größe hat, um es von Hand zu bauen.

## Der Plan im Kmila-Curriculum

Spätere Lektionen bauen ein AXI4-Lite-Register-Interface von Grund auf und verdrahten es dann zwischen deine 8-Bit-CPU und ein memory-mapped UART, sodass die CPU „drucken" kann, indem sie an eine Adresse schreibt. Das gibt dir das mentale Modell dafür, was in jedem modernen SoC vorgeht – in einer Größe, die du dir wirklich noch merken kannst.

## Was du behalten solltest

- Ein SoC = Recheneinheit(en) + Speicher + ein Interconnect, alles auf einem Die.
- Die meisten SoC-Kerne sind **RISC** mit einer **Harvard**-Teilung, sodass Code und Daten gleichzeitig auf getrennten Bussen laufen.
- Eine kurze **Pipeline** überlappt Fetch und Execute; **Sprünge flushen sie**.
- Programmspeicher ist nichtflüchtiger Code; Datenspeicher ist flüchtiges RAM; das **Programmiermodell** ist die Register- und Adresskarte, die der Programmierer sieht.
- Peripherie ist **memory-mapped** – die CPU erreicht sie mit gewöhnlichen Loads und Stores über ein Standard-Interconnect wie AXI4-Lite.
