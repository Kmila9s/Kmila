# AND gate

The **AND** gate outputs 1 only when **both** inputs are 1.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/04-and.html" title="Interactive logic gate on a module bench" loading="lazy"></iframe></div>

Move the cursor (or the arrow keys) to step through every input combination. The module computes the result and the output lamp **Y** lights according to the table.

In VHDL:

```vhdl
y <= a and b;
```

It is purely **combinational** — the output depends only on the current inputs, with no clock and no memory. Think of two switches in series: current reaches the lamp only if both are closed.
