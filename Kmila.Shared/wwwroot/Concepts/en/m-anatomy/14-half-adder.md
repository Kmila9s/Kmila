# Half adder

A **half adder** adds two bits A and B, producing a **sum** S and a **carry** C.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/14-half-adder.html" title="Interactive combinational block on a module bench" loading="lazy"></iframe></div>

Move the cursor (or the arrow keys) to step through every input combination. The input plungers set the bits, the module computes, the output lamps light, and the **current row of the table highlights** so you can read the function directly.

In VHDL:

```vhdl
s <= a xor b;   -- sum
c <= a and b;   -- carry
```

It is purely **combinational** — the outputs depend only on the current inputs, with no clock and no memory. The sum is an **XOR** and the carry is an **AND** — a half adder is literally those two gates side by side. It has no carry-in, so it is only the first stage.
