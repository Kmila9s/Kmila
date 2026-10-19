# Interconnect & I/O

The **interconnect** is the programmable wiring between cells: **routing channels**, **switch-boxes** at the crossings that connect them, and **I/O blocks (IOB)** at the edge that reach the pins.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/28-interconnect.html" title="Interactive figure" loading="lazy"></iframe></div>

A signal routes from an I/O block, along the channels and through the switch-boxes, to a CLB — the lit path. Programming the switch-boxes is what connects your design.

Most of an FPGA's area and delay is actually **routing**, not logic. Place-and-route decides which cells to use and how to wire them — the slow, clever step after synthesis.
