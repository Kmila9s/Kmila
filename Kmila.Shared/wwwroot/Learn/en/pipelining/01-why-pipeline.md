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

## Hazards — the cost

In practice you don't get 5×. Three kinds of collisions:

- **Data hazard** — instruction N+1 reads a register that instruction N is still writing. Fix: forwarding paths or stall cycles.
- **Control hazard** — instruction N is a branch, and we've already fetched N+1 before knowing whether to execute it. Fix: branch prediction or pipeline flush.
- **Structural hazard** — two stages need the same physical resource (e.g., one memory port). Fix: duplicate the resource, or serialize.

Every subsequent lesson in this module implements one fix at a time on the curriculum's 8-bit CPU, starting with forwarding.

## What to remember

- Pipelining trades **latency per instruction** (higher) for **throughput** (much higher).
- The clock speed-up is bounded by the longest stage, not the shortest.
- Hazards cost cycles; mitigations cost gates. Real CPU design is this trade-off at scale.

*More lessons coming — this module is under active authoring.*
