# Comparator

A **comparator** compares two numbers A and B and reports which is the case: A < B, A = B, or A > B.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/16-comparator.html" title="Interactive combinational block on a module bench" loading="lazy"></iframe></div>

Move the cursor (or the arrow keys) to step through every input combination. The input plungers set the bits, the module computes, the output lamps light, and the **current row of the table highlights** so you can read the function directly.

In VHDL:

```vhdl
lt <= '1' when a < b else '0';
eq <= '1' when a = b else '0';
gt <= '1' when a > b else '0';
```

It is purely **combinational** — the outputs depend only on the current inputs, with no clock and no memory. Exactly one of the three outputs is 1. Magnitude comparators scale up bit by bit, from the most significant bit down.
