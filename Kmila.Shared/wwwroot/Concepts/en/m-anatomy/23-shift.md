# Shift register

A **shift register** moves its bits one place along on every clock edge, a new bit entering at one end — a bucket brigade for data.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/23-shift.html" title="Interactive multi-bit sequential block, clock-driven" loading="lazy"></iframe></div>

A free-running clock ticks on the left; the 4-bit state is shown both as a row of **Q lamps** and as a live value (decimal + binary). On every clock edge the state updates; between edges it holds.

In VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= q(2 downto 0) & si;  -- shift left, SI in
  end if;
end process;
```

Shift registers do serial-to-parallel conversion (UART receive), multiply/divide by two, and build LFSRs for pseudo-random sequences.
