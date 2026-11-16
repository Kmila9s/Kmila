# Concurrent: everything fires together

Outside any process — directly inside an `architecture` between `begin` and `end` — you can write **concurrent signal assignments**. They look like sequential assignments but follow a completely different rule:

> **All concurrent statements that are sensitive to an event fire in the same delta cycle.** Source order is irrelevant.

Imagine a building with four light switches in different rooms. When the power comes on, every switch's state takes effect at the same instant — it doesn't matter which room the electrician wired first.

The player below has four concurrent statements driving four different outputs from the same inputs `a` and `b`. Press play and watch all four output lines update at the same step.

<!-- exec-fig:start -->
## Back to the restaurant: checking vs bells

There is still only **one waitress** here, so this is concurrency, not parallelism. Compare two ways she can juggle the tables:

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-restaurant.html?modes=poll,bell&mode=poll" title="Restaurant: one waitress checking every table vs answering bells" loading="lazy"></iframe></div>

- **1 ↻** — she walks round and **checks** every table, ready or not. Every ↻ on the readout is a wasted trip.
- **1 <svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><path d="M3 12H13M4.6 12C4.6 6 5.8 3 8 3S11.4 6 11.4 12M6.6 14.2A1.5 1.5 0 0 0 9.4 14.2"/></svg>** — she takes all the orders, then **only goes where a bell rings** (the bell on the table lights up when its dish is ready).

The VHDL simulator works the second way. It doesn't re-run every statement all the time: it only wakes up the processes whose **sensitivity list** received an **event**. The bell is the sensitivity list; the bell ringing is the event. Same waitress, same tables, fewer steps (about 23 s instead of 37 s, and 0 wasted checks).
<!-- exec-fig:end -->

<!-- exec-fig2:start -->
## The photographer: one snapshot, one flash

Why doesn't source order matter? Because in one delta cycle every concurrent statement reads the same **photo** of the inputs, and nobody sees anyone else's result until the **flash**.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-photo.html" title="Photographer studio: four desks copy from one photo; the flash flips all four boards together" loading="lazy" style="aspect-ratio:640/340"></iframe></div>

- The camera clicks: the photo freezes `a` and `b`.
- Each desk (one per statement) writes its **draft** from that same photo.
- The flash is the end of the delta: all four output boards flip **at the same time**.
<!-- exec-fig2:end -->

## Two ways to verify this

- **Mentally swap two lines** — does the result change? No. Try it.
- **Try removing the inputs from the architecture and see what happens to ALL outputs.** They all go back to `U` together, because there's no sequence — it's one parallel evaluation per delta cycle.

## Concurrent vs sequential — when do you use which?

| Use case | Pick |
|---|---|
| Combinational logic (gates, mux, arithmetic with no clock) | **Concurrent** |
| Clocked behavior (registers, FSMs, counters) | **Sequential** (process with `rising_edge(clk)`) |
| Read multiple signals, do math, output one result | Either works — concurrent reads more like a circuit, sequential reads more like a program |

> **Try this:** pause the player at the moment all four lines are highlighted. That single moment is **one delta cycle**. All four assignments are evaluating at exactly the same simulation time.
