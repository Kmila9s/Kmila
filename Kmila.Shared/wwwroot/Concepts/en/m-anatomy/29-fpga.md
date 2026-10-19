# FPGA — the whole chip

An **FPGA** is a sea of these parts on one chip: a grid of **CLBs** (logic cells), columns of **block RAM** and **DSP**, an **I/O ring**, a **clock tree**, and the routing that ties them together.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/29-fpga.html" title="Interactive figure" loading="lazy"></iframe></div>

The lit paths are two configured signals routing across the fabric. Everything you have seen in this track lives here: gates and adders become LUTs, registers become the flip-flops, and the router wires them through the channels.

That is the whole ladder in one chip: the **clock** paces it, **gates** and **adders** become **LUTs** inside **logic cells**, **flip-flops** hold state, **memory** and **DSP** sit in columns, and the **interconnect** wires it all — reprogrammable, because it is all look-up tables and switches loaded from a bitstream.
