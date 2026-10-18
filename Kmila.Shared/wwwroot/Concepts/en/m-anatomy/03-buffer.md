# Buffer (BUF)

The **buffer** copies its input to its output unchanged (Y = A). It changes no logic — it is used to **drive** a signal: to strengthen it or add a controlled delay.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/03-buffer.html" title="Interactive logic gate on a module bench" loading="lazy"></iframe></div>

Move the cursor (or the arrow keys) to step through every input combination. The module computes the result and the output lamp **Y** lights according to the table.

In VHDL:

```vhdl
y <= a;
```

It is purely **combinational** — the output depends only on the current inputs, with no clock and no memory. Notice there is no inversion bubble on the output — that is the only difference from a NOT gate.
