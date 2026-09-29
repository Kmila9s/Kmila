# Was ist ein System-on-Chip?

Deine 8-Bit-CPU aus dem vorigen Track läuft eigenständig. Sie rechnet, aber sie spricht nicht mit der Welt. Ein **System-on-Chip** ist die nächste Größenordnung: eine CPU plus eine Handvoll Peripheriebausteine plus das Geflecht, das sie miteinander reden lässt — alles auf demselben Die.

## Drei zwingende Bausteine

Jeder SoC, von der erweiterten Kmila-CPU bis zur 5-Millionen-Dollar-Apple-A-Serie, läuft auf folgendes hinaus:

1. **Ein oder mehrere Recheneinheiten** — CPUs, DSPs, GPUs, Beschleuniger.
2. **Speicher** — On-Chip-SRAM, Block-RAM, manchmal externer DDR.
3. **Ein Interconnect** — die Drähte + Arbitrierung, die (1) mit (2) und mit der Peripherie reden lässt.

Das Interconnect ist der Teil, der aufhört, „nur RTL“ zu sein, und architektonisch wird. Es regelt, wer wann reden darf, wie viele Transaktionen gleichzeitig unterwegs sein dürfen und wie schnell eine Anfrage abgeschlossen werden kann.

## Gängige Interconnect-Protokolle

Die Industrie hat sich auf eine Handvoll geeinigt:

| Protokoll       | Hersteller | Wo du es siehst                                        |
|-----------------|------------|--------------------------------------------------------|
| AXI4 / AXI4-Lite| ARM        | RISC-V-Kerne, Xilinx PS/PL-Grenze, fast alles Neue     |
| AHB / APB       | ARM        | Ältere ARM-Kerne, langsame Peripherie                  |
| Avalon MM / ST  | Intel      | NIOS-II, Qsys-generierte SoCs                          |
| Wishbone        | OpenCores  | OpenRISC, viele Open-Source-IP-Kerne                   |

Alle laufen auf dieselben drei Spuren hinaus: einen **Adresskanal**, einen **Schreibdatenkanal** und einen **Lesedatenkanal**. Unterschiede gibt es beim Handshake-Protokoll, bei Out-of-Order-Unterstützung und bei der Burst-Semantik.

## Was AXI4-Lite eigentlich ist

Das simpelste Mitglied der AXI-Familie. Lass alles über Bursts, Out-of-Order und QoS weg, und du bekommst fünf unabhängige Kanäle, jeder mit `valid`/`ready`-Handshake:

```
  AW  (Adresse schreiben)
  W   (Schreibdaten)
  B   (Schreibantwort)
  AR  (Adresse lesen)
  R   (Lesedaten)
```

Jeder Transfer ist ein Beat. Jeder Kanal hält an, wenn die Gegenseite nicht `ready` ist. Mehr nicht.

## Der Plan im Kmila-Curriculum

Spätere Lektionen in diesem Modul bauen ein AXI4-Lite-Register-Interface von Grund auf, verdrahten es dann zwischen deine 8-Bit-CPU und ein memory-mapped UART, sodass die CPU „drucken“ kann, indem sie an eine Adresse schreibt. Das gibt dir das mentale Modell dafür, was in jedem modernen SoC vorgeht — in einer Größe, die du dir wirklich noch merken kannst.

*Weitere Lektionen folgen — dieses Modul ist in aktiver Bearbeitung.*
