# Discrete-math prerequisites for digital design

Not every student arrives with the same math background. This lesson is a 25-minute refresh on the discrete-math ideas that show up constantly in digital design, so that when a later lesson says "think of this as a set" or "the state space is the Cartesian product of…", nothing stops you in your tracks. Everything here maps straight onto the four units of the ESCOM *Matemáticas Discretas* course — Logic, Set Theory, Integers, and Discrete Structures — and this time we read each one as hardware.

## Logic is digital design with other symbols

Propositional logic (the course's Unit I) is the same algebra your gates obey, just written with different glyphs. A **proposition** is a statement that is either true or false — exactly the two values a wire can carry.

| Math        | Reads as   | VHDL            |
|-------------|------------|-----------------|
| ∧ (and)     | "P and Q"  | `and`           |
| ∨ (or)      | "P or Q"   | `or`            |
| ¬ (not)     | "not P"    | `not`           |
| ⊕ (xor)     | "P xor Q"  | `xor`           |
| P ⇒ Q       | "if P then Q" | `(not P) or Q` |

That last row is worth memorising. The conditional **P ⇒ Q is logically equivalent to ¬P ∨ Q** — there is no `implies` gate because you never need one. Two more equivalences you will reach for constantly are **De Morgan's laws**:

- ¬(P ∧ Q) ≡ ¬P ∨ ¬Q  →  `not (a and b) = (not a) or (not b)`
- ¬(P ∨ Q) ≡ ¬P ∧ ¬Q  →  `not (a or b) = (not a) and (not b)`

De Morgan is why you can push a bubble (an inversion) through a gate and swap AND for OR. Synthesis tools do this thousands of times to fit your logic onto the cells a library actually provides.

**Watch out:** the converse (Q ⇒ P), the inverse (¬P ⇒ ¬Q), and the contrapositive (¬Q ⇒ ¬P) are *not* interchangeable. Only the **contrapositive is equivalent** to the original — a distinction the course drills in Unit I, and the reason a careless "if-else" can describe the wrong circuit.

## Boolean algebra: the logic you can solder

Unit IV calls this *álgebras y funciones booleanas*, and it is the formal core of combinational design. A **Boolean function** of n variables is a map from {0,1}ⁿ to {0,1}. There are exactly `2^(2^n)` of them for n inputs — 16 distinct 2-input functions, which is why the six named gates plus constants cover everything.

Every such function has two canonical forms:

- **Sum of minterms (SOP):** OR together one product term for each input row that gives a `1`.
- **Product of maxterms (POS):** AND together one sum term for each row that gives a `0`.

Worked example — the majority function (output `1` when two or three inputs are `1`):

```
f(a,b,c) = (a and b) or (a and c) or (b and c)
```

Those three products are the minterms. You can then shrink the expression with the Boolean axioms — identity, complement (`a or not a = 1`), distributivity, absorption (`a or (a and b) = a`) — or with a **Karnaugh map**, the visual minimiser from Unit IV. Fewer literals means fewer transistors.

```vhdl
y <= (a and b) or (a and c) or (b and c);  -- three gates, evaluated as wires
```

**Watch out:** minimising for *area* is not the same as minimising for *speed*, and a synthesiser may undo your hand-tuned algebra to balance both. Write clear logic; let the tool optimise.

## Sets: the vocabulary of signals, ports, and states

A **set** (Unit II) is an unordered collection of distinct elements: `{a, b, c}`. In digital design you meet them everywhere:

- The **values a signal can take.** `std_logic` is a 9-element set — `{U, X, 0, 1, Z, W, L, H, -}` — covering uninitialised, unknown, the two strong logic levels, high-impedance, weak levels, and don't-care.
- The **ports** an entity declares, and the **states** a finite-state machine can occupy.

The operations that recur are **union** (∪), **intersection** (∩), **difference** (∖), and **complement**. Two counting tools from Unit II earn their keep:

- **Cartesian product** A × B: the set of all ordered pairs. A 2-bit signal lives in `{0,1} × {0,1}`, so it has 4 values; an n-bit vector lives in `{0,1}ⁿ`, giving **2ⁿ**. This is literally why a 4-bit address reaches 16 locations.
- **Power set** 𝒫(A): the set of all subsets of A, with cardinality **2^|A|**. The truth table of an n-input function is an enumeration of 𝒫 over the inputs — same 2ⁿ rows.

For sizing overlapping sets, the **inclusion–exclusion principle** gives |A ∪ B| = |A| + |B| − |A ∩ B|.

## Functions and relations

Every combinational circuit **is a function**: given the inputs, there is exactly one correct output. `y <= a and b` realises `{(0,0)→0, (0,1)→0, (1,0)→0, (1,1)→1}`. Two properties (Unit IV, *propiedades de las funciones*) you will invoke without naming them:

- **Total** — every input has an output. In a combinational `process` you must assign the output on *every* path, or the tool infers an unwanted latch to "remember" the missing case.
- **Deterministic** — the same input always gives the same output, with no race conditions.

Sequential logic widens this to **relations** (Unit IV): a next-state relation can send one state to several, which is exactly a non-deterministic FSM — usually a bug, occasionally a specification you refine into a deterministic machine.

## Integers, bases, and induction

Unit III is number theory, and its *sistemas de numeración* section is pure digital groundwork: the base-change algorithm, the tight link between binary and hexadecimal (one hex digit = one 4-bit nibble), and arithmetic in base *b*. If base conversion or two's complement feels shaky, the **Number systems** lesson in the Digital Fundamentals module walks through it in detail.

Number theory also hands you tools you will use by name:

- **Divisibility and modular arithmetic.** A counter that rolls over at K is doing arithmetic *mod K*; clock dividers and address wrap-around are congruences (Unit IV, *enteros módulo n*).
- **Mathematical induction.** To prove an N-bit ripple-carry adder is correct, show the 1-bit case works, then that "if the N-bit version works, so does N+1." That is the shape of every `for … generate` loop that stitches a wide datapath from one repeated slice.

## Counting bits

Two formulas close the loop between all four units:

- A port of width N holds **2^N** distinct values (Cartesian product / power set).
- A machine with K states fits in **⌈log₂ K⌉** flip-flops with binary encoding, or **K** flip-flops with one-hot.

That is why a traffic-light FSM fits in 2 flip-flops, and why a 32-bit counter sees about 4 billion edges before it wraps.

## What to remember

- Propositional logic *is* Boolean algebra — same operators, and P ⇒ Q is just `(not P) or Q`. De Morgan lets you move inversions through gates.
- A Boolean function has canonical sum-of-minterms / product-of-maxterms forms; Karnaugh maps and the axioms shrink them.
- Sets name a circuit's values, ports, and states; **2ⁿ** (Cartesian product / power set) and **⌈log₂ K⌉** are the width formulas you will reuse forever.
- Combinational logic is a *total, deterministic* function — assign every output on every path or you get an accidental latch.
