# D latch

A **D latch** stores one bit. While its enable (here the clock) is high it is **transparent** — Q follows D; when the enable goes low it **freezes** the last value. This is the first **sequential** element in the ladder: unlike a gate, its output depends on the clock and on its own past, not just the current inputs.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/18-latch.html" title="Interactive sequential element with a live timing diagram" loading="lazy"></iframe></div>

This element has **memory**. Watch the timing diagram: the clock (**CLK**) ticks along the top, and the output **Q** only changes at the clock — in between it **holds** its value, no matter what the inputs do. Q is drawn in colour; the dashed lines mark the clock edges.

In VHDL:

```vhdl
process (en, d)
begin
  if en = '1' then   -- transparent while enabled
    q <= d;
  end if;
end process;
```

Because it is **level-sensitive**, a latch is transparent for a whole clock phase — any glitch on D during that window passes through. That is why synchronous design prefers edge-triggered flip-flops.
