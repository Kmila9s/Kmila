# Delta cycles and race conditions

A simulator needs to give "two things happening at the same time" a deterministic order. VHDL's answer is the **delta cycle**: an infinitesimal slice of time that separates cause from effect without advancing the clock on the wall. It's defined precisely in the VHDL standard's simulation cycle, and understanding it is what makes "weird" testbench behaviour suddenly make sense.

## What a delta cycle is

Simulation time advances in real steps (measured in ns, µs, ms). But within a single time step, changes happen in **delta-cycle** order:

- δ₀: an assignment schedules a signal update.
- δ₁: the update takes effect; any process sensitive to that signal now sees new inputs and wakes.
- δ₂: those woken processes schedule *their* updates.
- δ₃: those updates take effect; more processes may wake.
- …

Each delta is "zero real time" but still represents one causal step. The simulator keeps cycling through deltas until no signal changes any more — the circuit is **stable** — and only then does real simulation time advance to the next event.

## The simulation cycle, slightly more precisely

One pass through the engine is: (1) advance time to the next scheduled event; (2) update all signals scheduled for now; (3) run every process that was waiting on one of those signals, to completion, which may schedule new updates; (4) if any of those are for the *current* time, do another delta cycle; otherwise advance time. Signal updates in step 2 all happen *before* any process reads them in step 3 — that's the mechanism behind "signals update at the end of the step."

## Why this exists

Consider two signals that both update on the same clock edge:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= x;
    b <= y;
  end if;
end process;
```

Without delta cycles, the simulator would have to pick an arbitrary order — is `a` updated before `b`? After? Delta cycles sidestep this: both assignments are scheduled at δ₀ and both take effect at δ₁. Any process that reads `a` or `b` sees both new values together. The result no longer depends on the order the simulator happens to visit things.

## A data-flow chain

```vhdl
process(a)
begin
  b <= a;
end process;

process(b)
begin
  c <= b;
end process;
```

When `a` changes, the first process fires and schedules `b`. At the next delta, `b` has its new value, which wakes the second process, which schedules `c`. At the delta after that, `c` is right. Notice that `c` catches up to `a` after **two delta cycles** — real time never moved, but causality was preserved in order. Swap the two processes in the file and nothing changes: delta ordering guarantees the same final result. That is exactly why delta cycles exist.

## The race-condition trap

The problem is when you mix **signal** semantics with an assumption that "this must happen before that within the same tick":

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b + 1;     -- a updates at end of step
    b <= a + 1;     -- b reads the OLD a
  end if;
end process;
```

If you assumed "line 1 runs first, so `a` is already updated when line 2 runs," you're wrong. Both right-hand sides are evaluated using the **old** values, then both results are scheduled. `b` reads `a`'s old value, not the freshly computed one.

Replace the signals with variables to get C-style, read-what-you-just-wrote semantics:

```vhdl
process(clk)
  variable aa, bb : integer;
begin
  if rising_edge(clk) then
    aa := bb + 1;   -- immediate
    bb := aa + 1;   -- sees the new aa
  end if;
end process;
```

Now it works the way software programmers expect. (And the hardware differs: the signal version makes two parallel adders off the old registers; the variable version makes a dependent chain.)

## A classic real bug: the forgotten edge

Delta thinking also explains why this testbench stimulus misbehaves:

```vhdl
clk  <= not clk after 10 ns;
-- elsewhere, in a process with no wait:
data <= next_value;   -- same delta as the edge the DUT samples?
```

If a stimulus signal changes in the *same* delta the DUT clocks, whether the DUT sees the old or new value is fragile. The fix is to change stimulus a safe margin *away* from the active edge (or use `wait until rising_edge(clk)` and then a small delay). Many "it passes in sim but the ordering feels random" reports trace back to this.

## Spotting deltas in the tools

GHDL (the engine Kmila drives) and GTKWave both expose delta steps — a signal can take several delta cycles to ripple to its final value at a single time point. If a waveform shows a glitch that occupies "zero time," you're looking at intermediate delta values, not real hardware behaviour.

## The takeaway

- Delta cycles are the simulator's way of making concurrent logic deterministic without inventing fake time.
- Signal assignments take effect **after** the current delta; variables don't — they're immediate.
- Right-hand sides are evaluated with *old* signal values, then scheduled.
- If your testbench behaves "weird" around a clock edge, suspect a delta-ordering issue before you blame the tool.

## What to remember

- A delta cycle is zero real time but one causal step; the simulator runs deltas until the circuit is stable, then advances time.
- Signals update between deltas (so you read old values in the same pass); variables update instantly.
- Move testbench stimulus away from the active clock edge to avoid races.

The next exercise shows a small race condition in action. Find it, name it, fix it — you'll never forget the rule again.
