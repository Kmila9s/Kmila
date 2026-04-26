# What is a System-on-Chip?

Your 8-bit CPU from the earlier track runs standalone. It computes, but it doesn't talk to the world. A **System-on-Chip** is the next size up: a CPU plus a handful of peripherals plus the fabric that lets them talk to each other, all on the same die.

## Three mandatory pieces

Every SoC, from the Kmila CPU extended to a $5M Apple A-series, boils down to:

1. **One or more processing elements** — CPUs, DSPs, GPUs, accelerators.
2. **Memory** — on-chip SRAM, block RAM, sometimes off-chip DDR.
3. **An interconnect** — the wires + arbitration that lets (1) talk to (2) and to the peripherals.

The interconnect is the part that stops being "just RTL" and starts feeling architectural. It enforces who can talk when, how many transactions can be in flight, and how fast any given request can complete.

## Common interconnect protocols

The industry has settled on a handful:

| Protocol       | Vendor    | Where you'll see it                                    |
|----------------|-----------|--------------------------------------------------------|
| AXI4 / AXI4-Lite | ARM     | RISC-V cores, Xilinx PS/PL boundary, most new IP       |
| AHB / APB      | ARM       | Legacy ARM cores, low-speed peripherals                 |
| Avalon MM / ST | Intel     | NIOS-II, Qsys-generated SoCs                            |
| Wishbone       | OpenCores | OpenRISC, many open-source IP cores                     |

All of them boil down to the same three lanes: an **address channel**, a **write-data channel**, and a **read-data channel**. Variations are about handshake protocol, out-of-order support, and burst semantics.

## What AXI4-Lite actually is

The simplest member of the AXI family. Strip out everything about bursts, out-of-order, and QoS, and you get five independent channels, each with a `valid`/`ready` handshake:

```
  AW  (address write)
  W   (write data)
  B   (write response)
  AR  (address read)
  R   (read data)
```

Every transfer is one beat. Every channel stalls when the other side isn't `ready`. That's it.

## The Kmila curriculum plan

Later lessons in this module build an AXI4-Lite register interface from scratch, then wire it between your 8-bit CPU and a memory-mapped UART so the CPU can "print" by writing to an address. That gives you the mental model for what's happening inside every modern SoC, at a size you can actually hold in your head.

*More lessons coming — this module is under active authoring.*
