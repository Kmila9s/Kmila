# Demultiplexer (DEMUX)

A **demultiplexer** is the reverse of a MUX: it routes one input **D** to one of several outputs, chosen by the **select** line S.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/11-demux.html" title="Interactive combinational block on a module bench" loading="lazy"></iframe></div>

Move the cursor (or the arrow keys) to step through every input combination. The input plungers set the bits, the module computes, the output lamps light, and the **current row of the table highlights** so you can read the function directly.

In VHDL:

```vhdl
y0 <= d when s = '0' else '0';
y1 <= d when s = '1' else '0';
```

It is purely **combinational** — the outputs depend only on the current inputs, with no clock and no memory. The non-selected outputs stay at 0. Decoders and demultiplexers are close cousins — a demux is a decoder whose data input gates the active output.
