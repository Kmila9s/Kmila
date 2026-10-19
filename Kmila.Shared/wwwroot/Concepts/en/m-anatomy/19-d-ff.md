# D flip-flop (DFF)

A **D flip-flop** stores one bit, captured on the **rising edge** of the clock. Q takes the value of D at the instant of the edge and holds it until the next edge. This is the first **sequential** element in the ladder: unlike a gate, its output depends on the clock and on its own past, not just the current inputs.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/19-d-ff.html" title="Interactive sequential element with a live timing diagram" loading="lazy"></iframe></div>

This element has **memory**. Watch the timing diagram: the clock (**CLK**) ticks along the top, and the output **Q** only changes at the clock — in between it **holds** its value, no matter what the inputs do. Q is drawn in colour; the dashed lines mark the clock edges.

In VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= d;          -- sampled on the edge
  end if;
end process;
```

The DFF is **the** building block of synchronous design — every register, pipeline stage and state-machine bit is one or more DFFs. Put a MUX in front of D and you get a load-enable or a synchronous reset.
