# Logic cell (slice)

A **logic cell** (or *slice*) is the FPGA's repeating brick: a **LUT** + a **flip-flop** + an output **MUX** (plus a carry chain). It can give a combinational or a registered output.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/27-slice.html" title="Interactive figure" loading="lazy"></iframe></div>

Inputs feed the LUT; its result goes straight out (**combinational**) or through the flip-flop (**registered**); the MUX picks which. The clock drives the flip-flop; a carry chain crosses the tile.

Pack `y <= a and b` and a `rising_edge(clk)` register together and they fit in one slice — LUT for the logic, flip-flop for the state. Modern slices hold several LUTs and flip-flops.
