# NOT gate (inverter)

The **NOT gate** is the simplest logic there is: one input, one output, and the output is always the **opposite** of the input — 0 in gives 1 out, 1 in gives 0 out.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/02-not.html" title="Interactive NOT gate — a see-saw that inverts its input" loading="lazy"></iframe></div>

**Move the cursor** to set input **A** to 0 or 1. The figure is a see-saw with equal arms: push the input (A) down and the output (Y) rises, and vice versa — equal and opposite. The small circle on the output is the inversion bubble.

## The bubble

That **circle** on the output is the *inversion bubble*, the symbol of NOT in every schematic. Whenever you see it on a gate's output (NAND, NOR, XNOR…) it means the same thing: "and also, invert."

| A | Y |
|---|---|
| 0 | 1 |
| 1 | 0 |

In VHDL the inverter is the `not` operator:

```vhdl
y <= not a;
```

It is purely **combinational**: the output depends only on the current input — no clock, no memory. Change the input and the output flips after a small propagation delay.
