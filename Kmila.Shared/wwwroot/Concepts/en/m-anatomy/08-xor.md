# XOR gate (exclusive OR)

The **XOR** (exclusive-OR) gate outputs 1 when its inputs are **different**.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/08-xor.html" title="Interactive logic gate on a module bench" loading="lazy"></iframe></div>

Move the cursor (or the arrow keys) to step through every input combination. The module computes the result and the output lamp **Y** lights according to the table.

In VHDL:

```vhdl
y <= a xor b;
```

It is purely **combinational** — the output depends only on the current inputs, with no clock and no memory. XOR is the heart of addition (the sum bit), parity and comparison: it answers "are these two bits different?"
