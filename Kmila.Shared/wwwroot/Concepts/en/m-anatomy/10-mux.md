# Multiplexer (MUX)

A **multiplexer** routes one of several data inputs to the output. A **select** line **S** chooses which: here Y = D1 when S = 1, else D0.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/10-mux.html" title="Interactive combinational block on a module bench" loading="lazy"></iframe></div>

Move the cursor (or the arrow keys) to step through every input combination. The input plungers set the bits, the module computes, the output lamps light, and the **current row of the table highlights** so you can read the function directly.

In VHDL:

```vhdl
y <= d1 when s = '1' else d0;
```

It is purely **combinational** — the outputs depend only on the current inputs, with no clock and no memory. Every `if/then/else` and `case` in VHDL becomes a MUX after synthesis. It is also how a register's load-enable works: a MUX picks between the new value and holding the old one.
