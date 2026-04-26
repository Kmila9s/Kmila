# Was ist ein System-on-Chip?

Deine 8-Bit-CPU aus dem früheren Track läuft eigenständig. Sie rechnet, aber sie spricht nicht mit der Welt. Ein **System-on-Chip** ist die nächste Größe: eine CPU plus eine Handvoll Peripheriegeräte plus das Geflecht, das sie miteinander reden lässt, alles auf demselben Die.

## Drei verpflichtende Bestandteile

Jedes SoC, von der erweiterten Kmila-CPU bis zu einem Apple-A-Serie-Chip im Wert von 5 Mio. $, läuft hinaus auf:

1. **Ein oder mehrere Verarbeitungselemente** — CPUs, DSPs, GPUs, Beschleuniger.
2. **Speicher** — On-Chip-SRAM, Block-RAM, manchmal externes DDR.
3. **Ein Interconnect** — die Drähte + Arbitrierung, die (1) mit (2) und den Peripheriegeräten reden lassen.

Das Interconnect ist der Teil, der aufhört, "nur RTL" zu sein, und anfängt, sich architektonisch anzufühlen. Es erzwingt, wer wann reden darf, wie viele Transaktionen gleichzeitig unterwegs sein können und wie schnell eine gegebene Anfrage abgeschlossen werden kann.

## Gängige Interconnect-Protokolle

Die Industrie hat sich auf eine Handvoll geeinigt:

| Protokoll        | Hersteller | Wo du es sehen wirst                                    |
|------------------|------------|---------------------------------------------------------|
| AXI4 / AXI4-Lite | ARM        | RISC-V-Kerne, Xilinx PS/PL-Grenze, meistes neue IP      |
| AHB / APB        | ARM        | Ältere ARM-Kerne, langsame Peripherie                   |
| Avalon MM / ST   | Intel      | NIOS-II, Qsys-generierte SoCs                           |
| Wishbone         | OpenCores  | OpenRISC, viele Open-Source-IP-Kerne                    |

Alle laufen hinaus auf dieselben drei Spuren: einen **Adresskanal**, einen **Schreibdaten-Kanal** und einen **Lesedaten-Kanal**. Die Unterschiede betreffen das Handshake-Protokoll, Out-of-Order-Unterstützung und Burst-Semantik.

## Was AXI4-Lite eigentlich ist

Das einfachste Mitglied der AXI-Familie. Entferne alles zu Bursts, Out-of-Order und QoS, und du bekommst fünf unabhängige Kanäle, jeder mit einem `valid`/`ready`-Handshake:

```
  AW  (address write)
  W   (write data)
  B   (write response)
  AR  (address read)
  R   (read data)
```

Jeder Transfer ist ein Beat. Jeder Kanal stallt, wenn die andere Seite nicht `ready` ist. Das war's.

## Der Kmila-Lehrplan-Plan

Spätere Lektionen in diesem Modul bauen eine AXI4-Lite-Registerschnittstelle von Grund auf, verbinden sie dann zwischen deiner 8-Bit-CPU und einem speicherabgebildeten UART, damit die CPU "drucken" kann, indem sie auf eine Adresse schreibt. Das gibt dir das Denkmodell für das, was in jedem modernen SoC passiert, in einer Größe, die du dir tatsächlich merken kannst.

*Weitere Lektionen folgen — dieses Modul wird aktiv geschrieben.*
