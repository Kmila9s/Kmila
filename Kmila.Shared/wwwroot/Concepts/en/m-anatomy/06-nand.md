# NAND gate

The **NAND** gate is an AND followed by a NOT: it outputs 0 only when both inputs are 1.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/06-nand.html" title="Interactive logic gate on a module bench" loading="lazy"></iframe></div>

Move the cursor (or the arrow keys) to step through every input combination. The module computes the result and the output lamp **Y** lights according to the table.

In VHDL:

```vhdl
y <= a nand b;
```

It is purely **combinational** — the output depends only on the current inputs, with no clock and no memory. The bubble on the output marks the inversion. **NAND is universal**: any logic function can be built from NAND gates alone.
