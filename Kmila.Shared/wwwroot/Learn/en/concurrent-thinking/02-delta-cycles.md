# Delta cycles and race conditions

A simulator needs to give "two things happening at the same time" a deterministic order. VHDL's answer is the **delta cycle**: an infinitely small slice of time that separates cause from effect.

## What a delta cycle is

Simulation time advances in real steps (measured in ns, µs, ms). But within a single time step, changes can happen in **delta-cycle** order:

- δ₀: you schedule a signal update.
- δ₁: the update takes effect; any process sensitive to that signal now has new inputs.
- δ₂: those processes schedule their updates.
- δ₃: those updates take effect.
- …

Each delta is "zero real time" but still represents a causal step. The simulator keeps cycling through deltas until nothing else needs to update — then and only then does real time advance.

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

Without delta cycles, the simulator would have to pick an arbitrary order — is `a` updated before `b`? After? At the exact same instant? Delta cycles sidestep this: both assignments are scheduled at δ₀ and both take effect at δ₁. Any process that reads `a` or `b` sees both new values at the same time.

## The race-condition trap

Look at this:

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

When `a` changes, the first process fires and schedules `b`. At δ₁, `b` has its new value. The second process fires, schedules `c`. At δ₂, `c` has the right value.

But now swap the two processes. Does anything change? **No.** Delta cycles guarantee that both paths produce the same final result — even though the "order" of the processes in the file is different. That's exactly why delta cycles exist.

## Where things go wrong

The problem is when you mix **signal** semantics with "I need this to happen before that within the same tick". Example:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b + 1;     -- a updates at end of tick
    b <= a + 1;     -- b reads the OLD a
  end if;
end process;
```

If you assumed "line 1 runs first, so `a` is already updated when line 2 runs," you're wrong. `a` updates at the end of the tick. `b` reads `a`'s old value. Result: both increment based on old data, not on each other's new data.

Replace the signals with variables to get C-style semantics:

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

Now it works the way software programmers expect.

## The takeaway

- Delta cycles are the simulator's way of making concurrent logic deterministic.
- Signal assignments take effect **after** delta cycles. Variables don't.
- If your testbench behaves "weird" around a clock edge, suspect a delta-cycle ordering issue before you blame the tool.

The next exercise in the curriculum shows a small race condition in action. Find it, name it, fix it — you'll never forget the rule again.
