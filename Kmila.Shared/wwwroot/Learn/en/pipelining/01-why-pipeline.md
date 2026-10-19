# Why pipeline a CPU?

Your single-cycle 8-bit CPU from the earlier track works — but it's limited in one dimension that matters enormously on real hardware: **clock frequency**.

## The single-cycle ceiling

In a single-cycle design, the clock period has to be long enough for the longest-critical-path instruction to propagate all the way through: fetch → decode → register-read → ALU → memory → writeback, all combinationally. If the longest path is 20 ns, your maximum clock is 50 MHz. Period.

## Pipelining is dividing that path

Break the path into N stages separated by registers:

```
  [IF] → [ID] → [EX] → [MEM] → [WB]
    ^      ^      ^       ^       ^
    +------+------+-------+-------+
         registers between stages
```

The clock now only has to be long enough for the longest *stage* to complete, not the whole path. Five stages, each ~4 ns, gives a 250 MHz clock. Throughput goes from 1 instruction / 20 ns to 1 instruction / 4 ns — **5× speedup** for free, conceptually.

Notice what pipelining does *not* improve: the latency of any single instruction. One instruction still takes five stages to finish — about 20 ns, same as before (actually a touch more, because of the register setup time at each boundary). What changes is that a *new* instruction completes every cycle once the pipe is full. You're not making one car faster; you're turning one worker into an assembly line.

## The inter-stage registers

The registers between stages aren't bookkeeping — they *are* the pipeline. Each one latches everything the downstream stages still need: the partial result, the destination register number, the control signals decoded back in ID. Without them, stage N+1 would see stage N's inputs change mid-computation. These are the "registros inter etapa" the ESCOM syllabus (unit IV) asks you to implement in HDL.

## Why you never actually get 5×

The ideal is **CPI = 1** (one cycle per instruction) once the pipe is full. Two things erode it:

1. **Fill and drain.** The first instruction still needs all five stages before anything retires, so a short program pays a startup cost. For a long run this is negligible; for a tight loop it isn't.
2. **Hazards** — the real tax, and the subject of the rest of this module.

## Hazards — the cost

Three kinds of collision, each with its own fix:

**Structural hazard** — two stages want the same physical resource in the same cycle. The classic case: IF wants to read an instruction while MEM wants to read data, but there's only one memory port. Fix: split instruction and data memory (a Harvard split), or duplicate the port.

**Data hazard** — instruction N+1 needs a value instruction N hasn't written back yet:

```
  ADD R1, R2, R3   -- result ready at end of EX, written in WB
  SUB R4, R1, R5   -- needs R1 in its own EX, one cycle later
```

Without help, SUB reads the *old* R1. The fix is **forwarding** (also called bypassing): route the ALU result straight from N's EX/MEM output back to N+1's ALU input, instead of waiting for it to crawl through WB and back out of the register file. Forwarding kills most data stalls — but not all. A **load-use hazard** is the stubborn one: a load's value isn't available until the end of MEM, one stage *after* EX, so an instruction that uses it immediately still has to **stall one cycle** (insert a bubble) no matter how good your forwarding is.

**Control hazard** — instruction N is a branch, and the pipeline has already fetched N+1 (and maybe N+2) before it knows whether the branch is taken. Fixes:

- **Stall / flush** — freeze fetch until the branch resolves, or let the wrong-path instructions start and squash them if the guess was wrong. Resolving the branch earlier (in ID instead of EX) shrinks the penalty to a single bubble.
- **Static prediction** — always guess one way at compile time (e.g. "backward branches taken" catches loops well).
- **Dynamic prediction** — learn at run time with a table of **2-bit saturating counters** indexed by the branch's address. The counter needs *two* wrong guesses in a row before it flips its prediction, so a loop that's taken 99 times then exits once doesn't immediately change its mind. Predict "taken" when the counter is in its two high states, "not taken" in its two low states; nudge it toward taken on each taken branch and toward not-taken otherwise. Cheap, and it does almost as well as far more elaborate schemes.

Every subsequent lesson in this module implements one fix at a time on the curriculum's 8-bit CPU, starting with forwarding — matching the ESCOM progression of estructural → datos → bypassing/forwarding → riesgo de control.

## What to remember

- Pipelining trades **latency per instruction** (unchanged, even slightly higher) for **throughput** (much higher): ideal CPI → 1.
- The clock speed-up is bounded by the longest *stage*, not the whole path or the shortest stage.
- Inter-stage registers carry data *and* control forward; they're what makes the overlap safe.
- Forwarding erases most data hazards, but a load-use pair still costs one stall. Branches cost a flush unless you predict them — a 2-bit saturating counter is the workhorse predictor.
- Hazards cost cycles; mitigations cost gates. Real CPU design is this trade-off at scale.
