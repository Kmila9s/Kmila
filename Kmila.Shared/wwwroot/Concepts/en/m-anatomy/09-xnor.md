# XNOR gate (equivalence)

The **XNOR** gate outputs 1 when its inputs are **equal** — it is XOR with an inversion bubble.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/09-xnor.html" title="Interactive logic gate on a module bench" loading="lazy"></iframe></div>

Move the cursor (or the arrow keys) to step through every input combination. The module computes the result and the output lamp **Y** lights according to the table.

In VHDL:

```vhdl
y <= a xnor b;
```

It is purely **combinational** — the output depends only on the current inputs, with no clock and no memory. XNOR answers "are these two bits equal?" — it is the 1-bit equality (comparator) function.
