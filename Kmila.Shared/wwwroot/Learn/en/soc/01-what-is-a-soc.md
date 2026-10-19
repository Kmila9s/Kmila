# What is a System-on-Chip?

Your 8-bit CPU from the earlier track runs standalone. It computes, but it doesn't talk to the world. A **System-on-Chip** is the next size up: a CPU plus a handful of peripherals plus the fabric that lets them talk to each other, all on the same die.

This is the whole premise of the *Sistemas en Chip* course — instead of wiring a processor to separate memory chips and separate I/O chips on a board, you integrate the processor, its memory, and its peripherals into one device and program the result.

## Three mandatory pieces

Every SoC, from the Kmila CPU extended to a $5M Apple A-series, boils down to:

1. **One or more processing elements** — CPUs, DSPs, GPUs, accelerators.
2. **Memory** — on-chip SRAM, block RAM, sometimes off-chip DDR.
3. **An interconnect** — the wires + arbitration that lets (1) talk to (2) and to the peripherals.

The interconnect is the part that stops being "just RTL" and starts feeling architectural. It enforces who can talk when, how many transactions can be in flight, and how fast any given request can complete.

## The processing element: RISC and Harvard

Most SoC cores you'll meet — PIC, AVR, ARM Cortex-M, RISC-V — are **RISC** (Reduced Instruction Set Computer). RISC means a small set of simple, fixed-width instructions that each do one thing, so the hardware to decode and execute them stays small and fast. The trade is that complex operations take several instructions instead of one.

Those cores are usually **Harvard** architecture: program memory and data memory sit on *separate* buses. Because the two buses are independent, the core can fetch the next instruction at the same time it reads or writes a data operand — twice the memory bandwidth of a single shared bus. A nice side effect: the instruction word doesn't have to match the data word width. A PIC mid-range core, for example, runs 8-bit data but 14-bit instructions, so every instruction fits in a single word.

(A pure **von Neumann** machine shares one bus for code and data — simpler, but code and data fetches compete for it. The 8051 is a "modified Harvard" compromise.)

## Pipelining

A core overlaps work to finish more instructions per second. The simplest form is a two-stage **pipeline**: while instruction *N* executes, the core already fetches instruction *N+1*.

```
  cycle:   1        2        3        4
  fetch:  I1       I2       I3       I4
  exec:            I1       I2       I3
```

The catch is **branches**. When a `GOTO`, `CALL`, or `RETURN` changes the program counter, the instruction already fetched behind it is wrong and must be thrown away — the pipeline *flushes* and refills, costing a cycle. This is why tight loops and jump-heavy code run a little slower than straight-line code.

## Program memory, data memory, and the programming model

The ESCOM unit splits the memory map into two roles:

- **Program memory** holds your instructions (often flash/ROM). Non-volatile, so the device boots the same program every power-up.
- **Data memory** holds variables, the stack, and I/O registers (SRAM). Volatile, cleared on reset.

Layered on top is the **programming model**: the registers and address map the programmer actually sees. It defines the general-purpose registers, the special-function registers (status flags, the program counter, the stack pointer), and — crucially for an SoC — the **memory-mapped peripheral registers**. In a memory-mapped design a peripheral looks like a few addresses: write a byte to one address and the UART transmits it; read another address and you get the ADC result. The CPU never learns a new instruction for each peripheral; it just does loads and stores.

## Common interconnect protocols

The industry has settled on a handful of standards for that load/store fabric:

| Protocol         | Vendor    | Where you'll see it                              |
|------------------|-----------|--------------------------------------------------|
| AXI4 / AXI4-Lite | ARM       | RISC-V cores, Xilinx PS/PL boundary, most new IP |
| AHB / APB        | ARM       | Legacy ARM cores, low-speed peripherals          |
| Avalon MM / ST   | Intel     | NIOS-II, Qsys-generated SoCs                      |
| Wishbone         | OpenCores | OpenRISC, many open-source IP cores              |

All of them boil down to the same lanes: an **address channel**, a **write-data channel**, and a **read-data channel**. Variations are about handshake protocol, out-of-order support, and burst semantics.

## What AXI4-Lite actually is

The simplest member of the AXI family. Strip out bursts, out-of-order, and QoS, and you get five independent channels, each with a `valid`/`ready` handshake:

```
  AW  (address write)
  W   (write data)
  B   (write response)
  AR  (address read)
  R   (read data)
```

Every transfer is one beat. Every channel stalls when the other side isn't `ready`. That's it — which is exactly why it's the right size to build by hand.

## The Kmila curriculum plan

Later lessons build an AXI4-Lite register interface from scratch, then wire it between your 8-bit CPU and a memory-mapped UART so the CPU can "print" by writing to an address. That gives you the mental model for what's happening inside every modern SoC, at a size you can actually hold in your head.

## What to remember

- An SoC = processing element(s) + memory + an interconnect, all on one die.
- Most SoC cores are **RISC** with a **Harvard** split, so code and data move on separate buses at the same time.
- A short **pipeline** overlaps fetch and execute; **branches flush it**.
- Program memory is non-volatile code; data memory is volatile RAM; the **programming model** is the register and address map the programmer sees.
- Peripherals are **memory-mapped** — the CPU reaches them with ordinary loads and stores over a standard interconnect like AXI4-Lite.
