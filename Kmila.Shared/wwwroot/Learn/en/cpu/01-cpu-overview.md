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
