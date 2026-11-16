# Why VHDL can't do true parallel

You've now seen two models:

- **Sequential** — statements run one after another inside a process.
- **Concurrent** — statements outside a process all "fire together" in the same delta cycle.

Neither of those is the same as **parallel**. So what does parallel really mean?

> **Parallel** = multiple things happening *at the same instant of wall-clock time*.

When you put your VHDL on a real FPGA, that's exactly what happens. Every gate in the design settles its output as soon as its inputs are stable. There's no "step 1, step 2, step 3" — electrons flow through the silicon, and after a few nanoseconds the whole circuit is in its new state.

But when you press **Run** in Kmila, you're not on an FPGA. You're in a **simulator**, running on a single-threaded CPU. The simulator has to *pretend* the circuit is parallel, but it actually walks an event queue one event at a time.

```mermaid
flowchart LR
    subgraph hardware["On the FPGA (real parallel)"]
        G1[Gate AND]
        G2[Gate OR]
        G3[Gate XOR]
        G1 -.-> O1[output]
        G2 -.-> O1
        G3 -.-> O1
    end
    subgraph sim["In the simulator (single-threaded)"]
        S1[Event 1: a changed]
        S2[Event 2: re-evaluate AND]
        S3[Event 3: re-evaluate OR]
        S4[Event 4: re-evaluate XOR]
        S1 --> S2 --> S3 --> S4
    end
```

The simulator's job is to give you a result that **matches** what the hardware would do, not to actually run things in parallel. **Delta cycles** are the simulator's trick: they let it process many events "at the same simulation time" by giving every signal one chance to settle before time officially advances.

<!-- exec-fig:start -->
## In the restaurant: one waitress vs five

The best a single waitress can do is the bell strategy (**1 <svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><path d="M3 12H13M4.6 12C4.6 6 5.8 3 8 3S11.4 6 11.4 12M6.6 14.2A1.5 1.5 0 0 0 9.4 14.2"/></svg>**), and that is exactly what the simulator does. Real parallelism needs more waitresses (**5 ‖**):

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-restaurant.html?modes=bell,par&mode=bell" title="Restaurant: the simulator's single waitress vs five waitresses" loading="lazy"></iframe></div>

Let both runs finish and compare the dashed finish lines on the chart. The simulator can only **pretend** that everything happens at once, by putting events in order. The FPGA doesn't pretend: every gate is its own waitress, so every table really is served at the same time.
<!-- exec-fig:end -->

<!-- exec-fig2:start -->
## The conveyor belt vs the water pipes

Both halves of this bench compute the same three gates when `a` changes:

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-conveyor.html" title="Simulator conveyor with one robot arm vs hardware water pipes" loading="lazy" style="aspect-ratio:640/340"></iframe></div>

- **Back (simulator):** the change becomes **event crates** on a conveyor; a single robot arm handles them **one at a time** (AND, then OR, then XOR). The simulation clock still reads **0 ns**: they all count as the same instant.
- **Front (hardware):** opening the `a` valve sends water down all three channels at once, and the three gates light **together**. That is real parallelism.
<!-- exec-fig2:end -->

## The three models, lined up

| Model | Where it lives | "At the same time"? |
|---|---|---|
| Sequential | Inside a `process` body | No — one statement, then the next. |
| Concurrent | Outside any process, between `begin` and `end` | Yes — same delta cycle. |
| Parallel | **On real hardware only.** | Yes — same wall-clock nanosecond. |

## What about multicore simulators?

A few high-end VHDL simulators *do* use multiple CPU threads to evaluate large designs faster. But that's **parallelism for speed**, not parallelism in the language semantics. From the VHDL code's point of view, simulation is still single-threaded; the threading is an implementation detail invisible to you.

## Why we bother teaching this

Beginners write code that "should work in parallel" — and then they're surprised when the simulator gives a different answer than they expected. The next two lessons explain exactly *why*: it all comes down to **sensitivity lists** and **delta cycles**.
