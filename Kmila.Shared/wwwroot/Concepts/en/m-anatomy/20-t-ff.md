# T flip-flop

A **T flip-flop toggles**: on each clock edge, if T = 1 the stored bit flips (0↔1); if T = 0 it holds. It divides the clock frequency by two. This is the first **sequential** element in the ladder: unlike a gate, its output depends on the clock and on its own past, not just the current inputs.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/20-t-ff.html" title="Interactive sequential element with a live timing diagram" loading="lazy"></iframe></div>

This element has **memory**. Watch the timing diagram: the clock (**CLK**) ticks along the top, and the output **Q** only changes at the clock — in between it **holds** its value, no matter what the inputs do. Q is drawn in colour; the dashed lines mark the clock edges.

In VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if t = '1' then q <= not q; end if;
  end if;
end process;
```

Chain toggle flip-flops and each divides by two again — that is exactly a binary **ripple counter**. A T flip-flop is a DFF whose D is wired to Q xor T.
