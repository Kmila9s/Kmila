# Counter

A **counter** adds one to its stored value on every clock edge, cycling 0, 1, 2, … and wrapping around. It is a register with a +1 adder feeding back.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/24-counter.html" title="Interactive multi-bit sequential block, clock-driven" loading="lazy"></iframe></div>

A free-running clock ticks on the left; the 4-bit state is shown both as a row of **Q lamps** and as a live value (decimal + binary). On every clock edge the state updates; between edges it holds.

In VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= q + 1;   -- increment every edge
  end if;
end process;
```

Counters measure time and events, generate addresses, and divide clocks. Watch the lowest bit toggle every edge and each higher bit at half the rate — binary counting in motion.
