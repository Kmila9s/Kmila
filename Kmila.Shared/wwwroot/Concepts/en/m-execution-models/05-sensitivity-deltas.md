# Sensitivity lists & delta cycles

You've seen that signals don't update until "the delta cycle ends."
Now we'll meet the classic gotcha that comes from that rule.

A **sensitivity list** is the `( … )` after `process`. It tells the
simulator: *only wake this process when one of these signals changes*.

A **delta cycle** is the smallest unit of simulation time. Inside one
delta cycle, signal assignments only **schedule** new values — the
values themselves don't appear until the delta ends. During that time,
reading the signal gives you its **old** value.

Put those two rules together and you get the trap below. The player
runs a tiny process with two signal assignments:

```vhdl
process(a, b)
begin
  out1 <= a;
  out2 <= b and out1;   -- ← what does this read?
end process;
```

Looks innocent. The student usually thinks: "Line 3 sets `out1 = a`,
then line 4 reads `out1` and ANDs it with `b`." But line 4 reads the
**old** `out1` — the transaction from line 3 hasn't committed yet.

Watch the player and you'll see `out2` come out as `U` even though `a`
and `b` are both `1`. Surprising the first time.

## The two ways to fix it

### Option A — use a variable

Variables update immediately, so reading `tmp` on the next line gives
the just-assigned value:

```vhdl
process(a, b)
  variable tmp : std_logic;
begin
  tmp  := a;
  out1 <= tmp;
  out2 <= b and tmp;   -- reads the NEW tmp — works.
end process;
```

### Option B — wait for the second delta

Add `out1` to the sensitivity list. After the first delta commits
`out1`, the process wakes a second time and re-runs:

```vhdl
process(a, b, out1)
begin
  out1 <= a;
  out2 <= b and out1;   -- second wake reads the now-committed out1
end process;
```

Both options work; Option A is usually preferred because it's one delta
faster and the data flow is explicit.

> **Try this:** step through the player and watch the signal table.
> Even though `a=1` and `b=1`, `out2` ends as `U` — not `1`. That's
> the delta-cycle rule biting beginners every semester.
