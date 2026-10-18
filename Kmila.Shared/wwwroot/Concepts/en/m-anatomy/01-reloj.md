# The clock: the circuit's heartbeat

The **clock** is the signal that paces everything: a **square wave** that alternates between 0 and 1 at a **fixed frequency**. Before gates and flip-flops, it helps to see it move.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/01-reloj.html" title="Interactive clock — a toothed bar drawing a square wave" loading="lazy"></iframe></div>

**Move the cursor** left and right to change the frequency. The toothed bar is the waveform made physical: the follower rides up onto each tooth (level **1**) and drops into each gap (level **0**), while the flywheel sets a constant speed. The readout shows the current level and frequency.

## What really matters: the edge

Sequential logic — flip-flops, registers, counters — does not react to the clock's *level* but to its **edge**: the instant it jumps from 0 to 1 (**rising edge**) or 1 to 0 (**falling edge**). In VHDL:

```vhdl
process (clk)
begin
    if rising_edge(clk) then
        q <= d;          -- captured exactly on the rising edge
    end if;
end process;
```

- **Frequency** — cycles per second (Hz). Faster flywheel, tighter wave.
- **Period** — one full cycle: a tooth plus a gap.
- **Duty cycle** — the fraction of the period at 1; here 50%.
