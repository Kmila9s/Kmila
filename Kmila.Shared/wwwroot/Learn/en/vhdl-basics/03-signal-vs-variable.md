# Signal vs. variable

This is the single most misunderstood distinction in VHDL. Students read "a variable is like a local variable in C" and sprint off in the wrong direction. Let's ground it, because getting it right is what separates code that *simulates* from code that *becomes the hardware you intended*.

## Two kinds of names in VHDL

VHDL has **signals** (declared at the architecture level with `signal`) and **variables** (declared inside a process with `variable`). Both can hold values. Both can be assigned. They are **not** interchangeable.

| | signal | variable |
|---|---|---|
| Declared | architecture (or package) | inside a process/subprogram |
| Assignment operator | `<=` | `:=` |
| Takes effect | at the end of the simulation step | immediately |
| Scope | shared across the whole architecture | local to its process |
| Models | a wire or a register | a temporary computation |

## The golden rule

- **Signal assignments are *scheduled*; the value appears at the end of the current simulation step.**
- **Variable assignments happen immediately, right where the line sits.**

That's it. Every "gotcha" with signals vs variables is a restatement of this rule.

## The demo

```vhdl
architecture demo of ex is
  signal s : integer := 0;
begin
  process(clk)
  begin
    if rising_edge(clk) then
      s <= 1;           -- schedule: set s to 1 (not visible yet)
      s <= 2;           -- schedule: set s to 2 (overrides the first)
      -- what is s here?  Still 0. Neither assignment has taken effect.
    end if;
  end process;
end architecture demo;
```

After the clock edge completes, `s` becomes **2** — when a signal is scheduled several times in one process pass, only the *last* assignment wins. Inside the process, after both assignments, `s` is still `0`, because the new value doesn't propagate until the process finishes.

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
      -- v is 2 here, right now, during this pass
    end if;
  end process;
end architecture demo;
```

Notice two things:

- Variables use `:=`; signals use `<=`.
- Variables are local to a single process — you can't read `v` from another process or a concurrent assignment.

> One nuance about the initial value: `variable v : integer := 0;` runs its `:= 0` only **once**, when simulation starts — not on every process pass. A variable keeps its value between passes unless you reassign it. That's what makes it capable of *remembering*, and why a variable read before it is written in a clocked process can accidentally infer a register.

## Why this matters for hardware

A signal **models a wire or a flip-flop**. You can't change the value of a wire twice at the same instant — whatever drives the wire during one step settles to one value at the end of that step.

A variable **models a temporary computation** inside a process. The synthesizer typically implements it as combinational logic feeding into (or out of) the registers — or optimises it away entirely.

## A worked example: counting set bits

Variables shine when you need an intermediate result you'll update in a loop:

```vhdl
process(clk)
  variable ones : integer range 0 to 8;
begin
  if rising_edge(clk) then
    ones := 0;                      -- reset the accumulator each pass
    for i in data'range loop
      if data(i) = '1' then
        ones := ones + 1;           -- immediate update, read back next iteration
      end if;
    end loop;
    popcount <= ones;               -- publish the final count on a signal
  end if;
end process;
```

Try this with a signal instead of a variable and it breaks: `ones <= ones + 1` inside the loop would schedule eight competing assignments, only the last survives, and you'd always get `0` or `1`. The loop needs the *immediate* semantics of a variable.

## When to use which

- **Signals** — the default. Every port, every register, every bus you're going to see on the waveform viewer: signal.
- **Variables** — for local loop counters or intermediate arithmetic results inside one process, where you want C-like, read-what-you-just-wrote semantics.

## A common trap

Some students write:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b;    -- register 1
    b <= a;    -- register 2
  end if;
end process;
```

They worry it won't swap `a` and `b`. Because signal assignments are scheduled, it **does** swap: on the edge, `a` gets the **old** value of `b`, and `b` gets the **old** value of `a`, simultaneously. This is actually the idiomatic way to swap two registers in one cycle.

But switch to variables and the semantics change:

```vhdl
process(clk)
  variable a, b : integer;
begin
  if rising_edge(clk) then
    a := b;    -- a now equals b
    b := a;    -- b gets the NEW a — so b keeps its old value, a changes
  end if;
end process;
```

No swap. Both end up equal to the old `b`. Same-looking code, opposite result — all because of the golden rule.

## What to remember

- `signal`: value is scheduled, appears at the end of the step; `<=` assignment; models wires and registers; shared across the architecture.
- `variable`: updates immediately; `:=` assignment; local to a process; models intermediate computation.
- Multiple signal assignments in one pass → last one wins. Multiple variable assignments → they all run, in order.
- When in doubt — use a signal.
