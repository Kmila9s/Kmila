# Putting it together

Real designs use **both** concurrent and sequential code in the same
entity. The player below has a small entity with two pieces:

1. A **half-adder** — pure combinational logic written as two
   concurrent assignments. No clock, no process.
2. A **4-bit counter** — sequential, clocked logic written inside a
   process with `rising_edge(clk)`.

Both pieces live side by side in the same `architecture`. They use
different execution models because they answer different questions:

| Question | Model | Why |
|---|---|---|
| What's the sum of `a` and `b` *right now*? | Concurrent | No memory, no clock — pure gates. |
| What's the count *after the next clock tick*? | Sequential | We need a register that holds state between clock edges. |

## The two execution models, working together

When you press play, you'll see:

1. Inputs `a` and `b` change → both half-adder lines fire **together**
   in one delta cycle.
2. The clock rises → the counter process wakes and the `if rising_edge`
   guard lets us reach the increment.
3. The counter's `<= count + 1` is a signal assignment — it commits
   when the delta ends.

> **Why this matters in real designs:** every CPU you've ever used was
> built this way. Combinational logic decides "what would the answer
> be?", clocked registers decide "when do we commit it?". Master this
> split and you've got the entire mental model behind digital design.

## When in doubt: process or concurrent?

- **No clock involved? No memory needed?** Use concurrent assignments.
  They're cheaper to read, cheaper to reason about, and they
  synthesize to combinational gates.
- **Need to remember a value between clock edges?** Use a process
  guarded by `rising_edge(clk)`. That's how you get registers, FSMs,
  counters, accumulators — everything stateful.

The player has hit the end. You've now seen sequential, concurrent,
why true parallel doesn't apply, sensitivity-list traps, and the two
models cohabiting. Mark the lesson complete below — you've got the
mental model.
