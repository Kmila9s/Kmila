# Discrete-math prerequisites for digital design

Not every student arrives with the same math background. This lesson is a 20-minute refresh on the discrete-math ideas that show up constantly in digital design, so that when a later lesson says "think of this as a set" or "the state space is the Cartesian product of…", nothing stops you in your tracks.

## Sets

A **set** is an unordered collection of distinct elements: `{a, b, c}`. In digital design you'll meet sets in three places:

- The set of **possible values** a signal can take (`std_logic` is a 9-element set: `{U, X, 0, 1, Z, W, L, H, -}`).
- The set of **ports** an entity declares.
- The set of **states** a finite-state machine can be in.

Operations that'll come up: **union** (merge two sets), **intersection** (keep common), **difference** (remove), and the **Cartesian product** — the set of all ordered pairs from two sets. A 2-bit signal has `{0, 1} × {0, 1}` = 4 possible values.

## Functions

A **function** maps every input to exactly one output. Every combinational circuit is a function: given the inputs, there's one correct output. `y <= a and b` is the function `{(0,0)→0, (0,1)→0, (1,0)→0, (1,1)→1}`.

Two properties you'll invoke without thinking:

- **Total**: every input has an output (in a combinational process, you must assign on every path).
- **Deterministic**: same input → same output (no race conditions).

## Proof techniques

Three you'll lean on:

- **Truth tables**: exhaustive check. Works for small input spaces; our Lab 1 uses this.
- **Boolean algebra**: symbolic simplification (De Morgan, distributivity, complement).
- **Induction on bit-width**: prove an N-bit adder works by showing the 1-bit case and the "if N works, N+1 works" step. Shows up in recursive generics.

## Logic

Propositional logic IS digital design with different notation:

| Math        | VHDL          |
|-------------|---------------|
| ∧ (and)     | `and`         |
| ∨ (or)      | `or`          |
| ¬ (not)     | `not`         |
| ⊕ (xor)     | `xor`         |
| P ⇒ Q       | `(not P) or Q` |

If you've written a proof using these operators, you've written a circuit.

## Counting

Two formulas you'll use:

- A port of width N has **2^N** distinct values.
- A state machine with K states can be encoded in **⌈log₂ K⌉** flip-flops (for binary encoding) or **K** flip-flops (one-hot).

That's the reason a 4-bit address gives you 16 memory locations, why a traffic-light FSM fits in 2 flip-flops, and why a 32-bit counter covers 4 billion clock edges before it rolls over.

## What to remember

- Sets, functions, Cartesian products — the vocabulary of circuits.
- Boolean algebra is propositional logic you can solder.
- `⌈log₂ K⌉` is the bit width you need for K distinct things.
