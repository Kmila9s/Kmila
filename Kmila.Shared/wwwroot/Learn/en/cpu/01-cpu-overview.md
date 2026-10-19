# How a CPU executes one instruction

Look inside any CPU — from a tiny 8-bit microcontroller to a modern GPU shader core — and you'll find the same five stages, doing the same five jobs:

```mermaid
flowchart LR
    IF["IF<br/>fetch"] --> ID["ID<br/>decode"] --> EX["EX<br/>execute"] --> MEM["MEM<br/>memory"] --> WB["WB<br/>writeback"]
```

## The five classic stages

**IF (Instruction Fetch).** Read the next instruction from program memory at the address in the Program Counter (PC). Increment PC so that next cycle we're pointing at the following instruction.

**ID (Decode).** Crack open the instruction word. Split it into an **opcode** (what operation?) and **operand fields** (which registers? what immediate value?).

**EX (Execute).** Feed the operands into the ALU. For an `ADD R0, R1` instruction, EX reads R0 and R1 from the register file, tells the ALU "add", and latches the result. For a `JUMP addr` instruction, EX computes the new PC.

**MEM (Memory).** If the instruction is a load or store, this is where you talk to data memory. For register-to-register instructions, the stage is a pass-through — but the stage is always there so every instruction has the same pipeline depth.

**WB (Writeback).** The result finally lands in its destination — a register for arithmetic, the PC for a jump, a memory cell for a store.

## Single-cycle vs multi-cycle vs pipelined

The same five jobs can be arranged three different ways in silicon:

**Single-cycle.** All five stages execute combinationally within one clock period. Pros: simple. Cons: the clock has to be slow enough for the longest instruction (usually a load) to propagate through *everything* in one tick. Every simple MIPS teaching example is single-cycle.

**Multi-cycle.** One stage per clock cycle. The instruction sits in a state register while it moves through the stages. Pros: faster clock than single-cycle (each stage has its own short critical path). Cons: five cycles per instruction.

**Pipelined.** Five instructions in flight at once, each in a different stage. Pros: one new instruction retires every clock, so throughput ≈ single-cycle at a much faster clock. Cons: hazards (data, control, structural) that the rest of a Computer Architecture course is dedicated to. RISC-V cores, ARM Cortex-M, AMD Zen — all pipelined.

## RISC, CISC, and why the stages are so clean

Those five neat stages aren't an accident — they're what you get when the instruction set is designed to be regular. That's the **RISC** (Reduced Instruction Set Computer) philosophy the ESCOM course is built around:

- **Fixed-width instructions** (every instruction is the same number of bits), so fetch and decode are trivial.
- **Load/store architecture** — only dedicated load and store instructions touch memory; arithmetic works register-to-register. That's why MEM is a clean separate stage.
- **Many general-purpose registers**, so compilers keep values in the register file instead of spilling to memory.

**CISC** (Complex Instruction Set Computer, e.g. x86) takes the opposite bet: variable-length instructions, operations that read memory *and* compute in one instruction, and hundreds of addressing modes. Powerful per instruction, but much harder to pipeline cleanly. Modern x86 chips actually crack each complex instruction into RISC-like micro-ops internally.

One more split worth naming: **Von Neumann** machines keep program and data in one memory (one bus, simple, but code and data contend for it); **Harvard** machines use separate instruction and data memories, which is exactly what lets a single-cycle or pipelined core fetch an instruction *and* access data in the same tick. The CPU you're about to build is effectively Harvard — a ROM for code, a separate path for data.

## Instruction formats

Decode is only easy because each instruction's fields sit in fixed positions. Classic RISC (MIPS-style, the model ESCOM uses) has three formats:

- **R-type** — register-to-register ALU ops: `opcode | rs | rt | rd | shamt | funct`. Two source registers, one destination, a shift amount, and a function selector.
- **I-type** — immediate / load / store / branch: `opcode | rs | rt | immediate`. The 16-bit immediate is a constant or an address offset.
- **J-type** — jumps: `opcode | address`. A large address field for reaching far targets.

The toy ISA below is a two-field cousin of this idea: a 4-bit opcode and a 4-bit immediate. Same principle, smaller.

## The CPU project in this curriculum

Your first CPU will be **single-cycle** to keep it tractable. Everything happens combinationally inside one clock period:

- PC addresses a 16-byte ROM.
- The fetched byte is both the opcode nibble and the immediate nibble.
- Decode + execute + writeback all happen before the next rising clock edge, which captures the new PC and the new R0.

## The ISA we'll implement

Four instructions, 4-bit opcode + 4-bit immediate:

| Opcode | Mnemonic      | Semantics                  |
|--------|---------------|----------------------------|
| `0000` | `LOAD R0, #i` | `R0 <= zero_extend(i)`     |
| `0001` | `ADD  R0, #i` | `R0 <= R0 + zero_extend(i)`|
| `0010` | `JUMP addr`   | `PC <= addr`               |
| `1111` | `HALT`        | freezes PC and R0          |

A program you can already run by hand:

```
0: 03   LOAD R0, #3       -- R0 = 3
1: 14   ADD  R0, #4       -- R0 = 7
2: 12   ADD  R0, #2       -- R0 = 9
3: F0   HALT
```

## What this gives you

Once the scaffold works, you have a **programmable machine**. Change the bytes in the ROM — change the program the CPU runs. That's the fundamental trick: the same hardware, different contents in the memory, different behavior.

From here you can grow:

- Add more registers (R0..R3).
- Add conditional jumps (`JZ`, `JNZ`) so your programs can branch.
- Add loads/stores from a RAM module (reuse the one from the Memories module).
- Pipeline the whole thing.

The path from this 4-instruction machine to a full RV32I RISC-V core is long, but every step is just more of the same pattern you're about to build.

## Who steers all this: the control unit

The datapath (PC, ROM, register file, ALU, memory) is the plumbing; the **control unit** is what opens the right valves each cycle. It reads the opcode and drives the control signals: which ALU operation, whether the register file writes this cycle, whether memory reads or writes, where the next PC comes from. Two ways to build it:

- **Hardwired control** — the opcode feeds combinational logic (or a tiny state machine) that produces the signals directly. Fast, and the natural choice for a clean RISC. This is what your scaffold does with its `case` on the opcode.
- **Microcoded control** — each instruction is a short program of *microinstructions* stored in a microcode ROM, which steps out the control signals. Flexible and historically how CISC machines tamed their complexity, at the cost of speed. The ESCOM syllabus names both (microinstrucciones, memorias de microcódigo) so you can tell them apart.

Alongside sits the **status register** — the flags your ALU produced (zero, carry, overflow, negative). Conditional branches are just the control unit testing one of those flags to decide the next PC.

## What to remember

- Every CPU, big or small, runs the same loop: fetch, decode, execute, memory, writeback.
- Those stages can be single-cycle (simple, slow clock), multi-cycle (faster clock, several cycles each), or pipelined (overlapped, highest throughput).
- RISC keeps instructions fixed-width and load/store so decode and pipelining stay clean; CISC trades that away for power per instruction.
- The datapath moves the data; the control unit — hardwired or microcoded — decides what moves where, using the opcode and the status flags.
