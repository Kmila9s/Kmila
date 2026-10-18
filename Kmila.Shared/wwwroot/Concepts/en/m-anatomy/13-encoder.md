# Encoder

An **encoder** is the reverse of a decoder: given a one-hot input (one line active), it outputs the **binary address** of that line.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/13-encoder.html" title="Interactive combinational block on a module bench" loading="lazy"></iframe></div>

Move the cursor (or the arrow keys) to step through every input combination. The input plungers set the bits, the module computes, the output lamps light, and the **current row of the table highlights** so you can read the function directly.

In VHDL:

```vhdl
a <= "11" when i3 = '1' else
     "10" when i2 = '1' else
     "01" when i1 = '1' else
     "00";
```

It is purely **combinational** — the outputs depend only on the current inputs, with no clock and no memory. A **priority** encoder resolves several active inputs by taking the highest — useful for interrupt controllers that must pick one request.
