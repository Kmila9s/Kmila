# Register

A **register** is a bank of flip-flops that stores a whole word at once. On the clock edge (when loaded) all bits of D are captured into Q together.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/22-register.html" title="Interactive multi-bit sequential block, clock-driven" loading="lazy"></iframe></div>

A free-running clock ticks on the left; the 4-bit state is shown both as a row of **Q lamps** and as a live value (decimal + binary). On every clock edge the state updates; between edges it holds.

In VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if ld = '1' then q <= d; end if;  -- load on edge
  end if;
end process;
```

Registers are where a CPU keeps its working values, and every pipeline stage is a register. Put a MUX in front (new value vs. current Q) and the load-enable becomes a hold.
