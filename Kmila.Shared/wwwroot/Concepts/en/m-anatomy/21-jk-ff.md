# JK flip-flop

A **JK flip-flop** is the most general: on each edge J sets Q to 1, K resets it to 0, both together **toggle** it, and neither holds. This is the first **sequential** element in the ladder: unlike a gate, its output depends on the clock and on its own past, not just the current inputs.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/21-jk-ff.html" title="Interactive sequential element with a live timing diagram" loading="lazy"></iframe></div>

This element has **memory**. Watch the timing diagram: the clock (**CLK**) ticks along the top, and the output **Q** only changes at the clock — in between it **holds** its value, no matter what the inputs do. Q is drawn in colour; the dashed lines mark the clock edges.

In VHDL:

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if    j='1' and k='1' then q <= not q;
    elsif j='1' then q <= '1';
    elsif k='1' then q <= '0';
    end if;
  end if;
end process;
```

The JK removes the forbidden state of the older SR latch. Any flip-flop can be built from any other — DFF, T and JK are interchangeable with a little logic in front.
