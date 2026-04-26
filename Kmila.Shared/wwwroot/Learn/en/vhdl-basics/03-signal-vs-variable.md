# Signal vs. variable

This is the single most misunderstood distinction in VHDL. Students read "a variable is like a local variable in C" and sprint off in the wrong direction. Let's ground it.

## Two kinds of names in VHDL

VHDL has **signals** (declared at the architecture level with `signal`) and **variables** (declared inside a process with `variable`). Both can hold values. Both can be assigned. They are **not** interchangeable.

## The golden rule

- **Signal assignments happen at the end of the current simulation tick.**
- **Variable assignments happen immediately, inside the process.**

That's it. Every "gotcha" with signals vs variables is a restatement of this rule.

## The demo

```vhdl
architecture demo of ex is
  signal s : integer := 0;
begin
  process(clk)
  begin
    if rising_edge(clk) then
      s <= 1;           -- request: set s to 1 (not visible yet)
      s <= 2;           -- request: set s to 2 (overrides the first)
      -- what is s here?  Still 0. Neither assignment has taken effect.
    end if;
  end process;
end architecture demo;
```

After the clock edge completes, `s` becomes **2** (only the last assignment "wins" when multiple happen in the same tick). Inside the process, after both assignments, `s` is still `0` — because the new value doesn't propagate until the process is done.

Now with a variable:

```vhdl
architecture demo of ex is
begin
  process(clk)
    variable v : integer := 0;   -- variable lives inside the process
  begin
    if rising_edge(clk) then
      v := 1;           -- takes effect immediately
      v := 2;           -- takes effect immediately
      -- v is 2 here, right now, during this tick
    end if;
  end process;
end architecture demo;
```

Notice two things:

- Variables use `:=`; signals use `<=`.
- Variables are local to a single process.

## Why this matters for hardware

A signal **models a wire or a flip-flop**. You can't change the value of a wire twice at the same instant — whatever drives the wire during one tick settles to one value at the end of that tick.

A variable **models a temporary computation** inside a process. The synthesizer is free to implement it as combinational logic, or optimise it away entirely.

## When to use which

- **Signals** — the default. Every port, every register, every bus you're going to see on the waveform viewer: signal.
- **Variables** — for local loop counters or intermediate arithmetic results inside one process, where you want C-like semantics inside that process.

## A common trap

Some students write:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b;    -- register 1
    b <= a;    -- register 2 — "swaps" a and b
  end if;
end process;
```

They expect this to swap `a` and `b`. Because signal assignments happen at the end of the tick, it **does**: on the edge, `a` gets the **old** value of `b`, and `b` gets the **old** value of `a`. This is actually the idiomatic way to swap two registers in one cycle.

But if you switch to variables the semantics change:

```vhdl
process(clk)
  variable a, b : integer;
begin
  if rising_edge(clk) then
    a := b;    -- a now equals b
    b := a;    -- b gets the NEW a — so b keeps its old value, and a changes
  end if;
end process;
```

No swap. Just both equal to the old `b`.

## What to remember

- `signal`: updates at end of tick. `<=` assignment. Models wires and registers.
- `variable`: updates immediately. `:=` assignment. Local to a process. Models intermediate computation.
- When in doubt — use a signal.
