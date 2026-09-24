# What does "run" mean?

When you press **Run** in Kmila, the simulator doesn't actually power up an
FPGA. It walks through your VHDL one step at a time — but **how** it walks
depends on where the code lives.

There are three execution models people mix up all the time:

| Model | Meaning | Where you see it in VHDL |
|---|---|---|
| **Sequential** | One statement at a time, in source order. | Inside a `process` body. |
| **Concurrent** | Many statements evaluate "together" — order in the file is irrelevant. | Concurrent statements in an architecture (between `begin` and `end`). |
| **Parallel** | Multiple things actually happening at the same instant in real wall-clock time. | **Real hardware only.** Never in simulation. |

The example below is intentionally tiny — a process that reads two inputs
`a` and `b` and writes one output `sum`. Press ▶ on the player and watch
which line the simulator is "looking at" right now.

```text
Sequential:   line 1  →  line 2  →  line 3  …
Concurrent:   lines 1 and 2 and 3 all evaluate this instant
Parallel:     not a thing in simulation. Only happens on the FPGA itself.
```

## Why this matters

Beginners often read `sum <= b;` and think the value of `sum` becomes `b`
**immediately**, the way `sum = b;` would in C. It doesn't. The assignment
**posts a transaction** and the new value only becomes visible after the
current delta cycle ends.

The player on the right hardcodes a short execution — there's no real
simulation running. That keeps the explanation honest: every step carries a
teaching note instead of you having to guess what the live simulator was
thinking.

> **Try this:** step forward manually and watch `sum` stay at `U`
> ("uninitialized") even after step 3 — it only changes when the delta cycle
> closes at step 4.
