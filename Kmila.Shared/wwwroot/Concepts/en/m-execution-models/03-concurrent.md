# Concurrent: everything fires together

Outside any process — directly inside an `architecture` between `begin`
and `end` — you can write **concurrent signal assignments**. They look
like sequential assignments but follow a completely different rule:

> **All concurrent statements that are sensitive to an event fire in the
> same delta cycle.** Source order is irrelevant.

Imagine a building with four light switches in different rooms. When the
power comes on, every switch's state takes effect at the same instant —
it doesn't matter which room the electrician wired first.

The player below has four concurrent statements driving four different
outputs from the same inputs `a` and `b`. Press play and watch all four
output lines update at the same step.

## Two ways to verify this

- **Mentally swap two lines** — does the result change? No. Try it.
- **Try removing the inputs from the architecture and see what happens
  to ALL outputs.** They all go back to `U` together, because there's no
  sequence — it's one parallel evaluation per delta cycle.

## Concurrent vs sequential — when do you use which?

| Use case | Pick |
|---|---|
| Combinational logic (gates, mux, arithmetic with no clock) | **Concurrent** |
| Clocked behavior (registers, FSMs, counters) | **Sequential** (process with `rising_edge(clk)`) |
| Read multiple signals, do math, output one result | Either works — concurrent reads more like a circuit, sequential reads more like a program |

> **Try this:** pause the player at the moment all four lines are
> highlighted. That single moment is **one delta cycle**. All four
> assignments are evaluating at exactly the same simulation time.
