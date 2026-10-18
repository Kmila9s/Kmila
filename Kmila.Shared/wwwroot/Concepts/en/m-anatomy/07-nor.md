# NOR gate

The **NOR** gate is an OR followed by a NOT: it outputs 1 only when both inputs are 0.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/07-nor.html" title="Interactive logic gate on a module bench" loading="lazy"></iframe></div>

Move the cursor (or the arrow keys) to step through every input combination. The module computes the result and the output lamp **Y** lights according to the table.

In VHDL:

```vhdl
y <= a nor b;
```

It is purely **combinational** — the output depends only on the current inputs, with no clock and no memory. The bubble marks the inversion. **NOR is also universal**: any circuit can be built from NOR gates alone.
