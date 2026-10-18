# Full adder

A **full adder** adds three bits — A, B and a carry-in **Ci** — producing a sum S and a carry-out **Co**. Chained, they add wide numbers.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/15-full-adder.html" title="Interactive combinational block on a module bench" loading="lazy"></iframe></div>

Move the cursor (or the arrow keys) to step through every input combination. The input plungers set the bits, the module computes, the output lamps light, and the **current row of the table highlights** so you can read the function directly.

In VHDL:

```vhdl
s  <= a xor b xor ci;
co <= (a and b) or (ci and (a xor b));
```

It is purely **combinational** — the outputs depend only on the current inputs, with no clock and no memory. The full adder is the building block of every binary adder: chain n of them, carry to carry, and you add two n-bit numbers. It is built from two half adders and an OR.
