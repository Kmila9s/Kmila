# What does "run" mean?

When you press **Run** in Kmila, the simulator doesn't actually power up an FPGA. It walks through your VHDL one step at a time — but **how** it walks depends on where the code lives.

There are three execution models people mix up all the time:

| Model | Meaning | Where you see it in VHDL |
|---|---|---|
| **Sequential** | One statement at a time, in source order. | Inside a `process` body. |
| **Concurrent** | Many statements evaluate "together" — order in the file is irrelevant. | Concurrent statements in an architecture (between `begin` and `end`). |
| **Parallel** | Multiple things actually happening at the same instant in real wall-clock time. | **Real hardware only.** Never in simulation. |

The example below is intentionally tiny — a process that reads two inputs `a` and `b` and writes one output `sum`. Press ▶ on the player and watch which line the simulator is "looking at" right now.

```text
Sequential:   line 1  →  line 2  →  line 3  …
Concurrent:   lines 1 and 2 and 3 all evaluate this instant
Parallel:     not a thing in simulation. Only happens on the FPGA itself.
```

<!-- exec-fig:start -->
## The restaurant analogy

Picture a restaurant with **10 tables**. The **waitress** is whoever does the work (a CPU, or Kmila's simulator). Each **table** is a piece of work that needs attention, and each **dish** takes a while to cook. Press the buttons above the drawing to change how the restaurant is run:

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-restaurant.html?modes=seq,poll,par" title="Restaurant: one waitress, one waitress juggling tables, five waitresses" loading="lazy"></iframe></div>

- **1 →** — *sequential*: one waitress serves one table at a time. She takes the order and **stands there until the dish arrives**, even if nine other tables are waiting.
- **1 ↻** — *concurrent*: still one waitress, but if a dish isn't ready she **moves on**: takes the next order, comes back to check the first one, serves whatever is ready.
- **5 ‖** — *parallel*: five waitresses, two tables each, all working **at the same instant**.

How to read the chart: one row per table; **blue bar** = waitress at that table, **red line** = dish cooking, **dashed** = dish ready but not served yet, **green dot** = served. The readout shows <svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><circle cx="8" cy="9" r="5.5"/><path d="M8 9V6.2M6.5 1.8H9.5M8 1.8V3.5"/></svg> elapsed time, <svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><path d="M4.5 2H11.5M4.5 14H11.5M5.2 2C5.2 5.5 10.8 6.8 10.8 14M10.8 2C10.8 5.5 5.2 6.8 5.2 14"/></svg> time the waitress stood idle, ↻ "is it ready?" checks and ✓ tables served. After each run, a dashed finish line stays on the chart so you can compare the modes.

| Restaurant | Execution model |
|---|---|
| Waitress | The thing that executes: a CPU thread, or the simulator |
| Table | A `process` (or a concurrent statement) |
| Dish cooking | Time passing (`after 10 ns`, waiting for a clock edge…) |
| Dish ready | An **event**: something the process was waiting for happened |
| Five waitresses | Real hardware: an FPGA, where every piece of the circuit is its own "waitress" |

> **Notice:** the concurrent waitress doesn't walk faster — she simply **never stands idle**. Sequential takes about 70 s, concurrent about 37 s, parallel about 12 s.
<!-- exec-fig:end -->

## Why this matters

Beginners often read `sum <= b;` and think the value of `sum` becomes `b` **immediately**, the way `sum = b;` would in C. It doesn't. The assignment **posts a transaction** and the new value only becomes visible after the current delta cycle ends.

The player on the right hardcodes a short execution — there's no real simulation running. That keeps the explanation honest: every step carries a teaching note instead of you having to guess what the live simulator was thinking.

> **Try this:** step forward manually and watch `sum` stay at `U` ("uninitialized") even after step 3 — it only changes when the delta cycle closes at step 4.
