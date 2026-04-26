# Hardware is parallel

Students coming from C, Python, or JavaScript have all internalised the same mental model: "code runs top to bottom, one statement at a time." VHDL will punish you for that assumption until you build the correct mental model.

## The big shift

Look at this architecture:

```vhdl
architecture rtl of demo is
begin
  y1 <= a and b;
  y2 <= a or  b;
  y3 <= y1 xor y2;
end architecture rtl;
```

In a software language, you'd read this as three sequential steps. In VHDL, all three lines describe **wires that exist simultaneously**. They don't execute in order — they exist.

The synthesizer builds:

```
   a ─┬─ AND ─── y1 ──┐
      │               XOR ── y3
   b ─┴─ OR  ── y2 ───┘
```

All three gates are powered up at the same time. When `a` or `b` changes, the output `y3` settles to its new value after a small propagation delay through whichever path is longest. There is no "first" and "second".

## Why the order doesn't matter

You could write the three lines in any order:

```vhdl
y3 <= y1 xor y2;   -- reference before "declaration"?
y1 <= a and b;
y2 <= a or  b;
```

This is still valid VHDL. The three assignments describe three wires, and wires don't care what order you drew the schematic in. The tool reads all three and wires them up.

## Where "sequential" sneaks back in

Inside a `process`, things **are** sequential — within that process, one statement at a time, top to bottom. But a process as a whole is still just one more concurrent unit next to all the others.

```vhdl
architecture rtl of demo is
begin

  -- Concurrent 1
  y1 <= a and b;

  -- Concurrent 2 (a process, which internally is sequential)
  process(clk)
  begin
    if rising_edge(clk) then
      counter <= counter + 1;
    end if;
  end process;

  -- Concurrent 3
  y2 <= a or b;

end architecture rtl;
```

All three concurrent units are alive at the same time. Each one runs whenever its inputs change. Inside the process, the body is read in order, but that's a local property of the process, not of the architecture.

## A concrete consequence

This design has a subtle race:

```vhdl
architecture wrong of demo is
  signal a, b : std_logic;
begin
  a <= not b;
  b <= not a;
end architecture wrong;
```

Read as software, this looks like "set `a` to not-b, then set `b` to not-a" — no problem. Read as hardware, it's two inverters wired into a loop. That's an **oscillator** — a ring of feedback that can never settle. Simulation will complain, synthesis will (sometimes) optimise it away, and the board will glitch unpredictably.

## The rewiring in your head

- Stop thinking "what runs first?"
- Start thinking "what wires exist?"
- Inside a process, the old "one-at-a-time" model is back — but only inside that process.

Once this clicks, VHDL stops fighting you. You'll read an architecture and immediately see the schematic it represents, not the "program execution" you used to imagine.
