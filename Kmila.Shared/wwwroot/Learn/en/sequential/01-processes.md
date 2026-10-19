# Processes and sensitivity lists

Everything in VHDL outside of a process is concurrent — wires and gates. The moment you want a circuit to **remember** something from one clock cycle to the next, you reach for a process. Processes are where the "sentencias secuenciales" of the HDL unit live, and they're the bridge from combinational logic (Unit III) to the memory elements (Unit IV).

## What a process is

A `process` is a block of sequential code inside a VHDL architecture. Its lines are read top to bottom, just like software. But the process itself only "runs" when something in its **sensitivity list** changes — the list is the process's wake-up condition.

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= '0';
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

This block means: "whenever `clk` or `reset` changes, re-evaluate my body." Between those events the process sleeps and `q` holds its value — which is exactly the behaviour of a register.

## Two shapes you'll see 99% of the time

### Combinational process

Sensitivity list = **every signal read** inside. No clock. Output is a pure function of the inputs.

```vhdl
process(a, b, sel)
begin
  if sel = '0' then
    y <= a;
  else
    y <= b;
  end if;
end process;
```

Synthesizes to a 2-to-1 mux. No storage — provided every output is assigned on every path (more on that in the latch fix-it).

### Clocked (sequential) process

Sensitivity list = `(clk, reset)` for an async reset, or just `(clk)` for a sync reset. The data assignments happen only inside `rising_edge(clk)` (or `falling_edge`). Output is **registered** — it changes only on clock edges.

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= (others => '0');
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

This is the classic D flip-flop pattern, and it's the template for essentially every register you'll ever write.

## `rising_edge` vs. the old `clk'event` idiom

You'll see two ways to detect a clock edge:

```vhdl
elsif rising_edge(clk) then        -- preferred
elsif clk'event and clk = '1' then -- older, equivalent for '0'/'1'
```

`rising_edge(clk)` (from `std_logic_1164`) is the modern, clearer form. It also correctly ignores edges that involve metavalues (like `'X'` → `'1'`), whereas the raw `clk'event and clk='1'` can misfire on them. Use `rising_edge`.

## The sensitivity-list trap

If you forget to list a signal that a *combinational* body reads, simulation and synthesis disagree:

- **Simulation**: the process won't re-evaluate when that signal changes. The waveform looks wrong (outputs appear "stuck").
- **Synthesis**: the tool reads the body, sees every read, and builds the right hardware anyway — ignoring your list.

Result: your testbench and your board behave differently, which is the worst kind of bug. (Clocked processes are immune to this particular trap because they only ever act on the clock edge, but an incomplete list on a *combinational* process is a classic cause of phantom failures.) VHDL-2008 fixes it outright with `process(all)`:

```vhdl
process(all)  -- the tool includes every signal read in the body
begin
  ...
end process;
```

Kmila's parser accepts `process(all)`, and it's what most teams use for combinational processes in new designs.

## A rule of thumb

- Sensitivity = just `(clk)` or `(clk, reset)` → you're describing **registers** (flip-flops).
- Sensitivity = every input you read (or `all`) → you're describing **combinational logic**.
- Sensitivity mixes a clock edge with extra data signals you then use *outside* the edge → you've almost certainly got a bug.

## One more: don't drive the same signal from two processes

A signal driven inside a process has that process as its driver. If a *second* process (or a concurrent statement) also drives it, you've created multiple drivers — resolved to `'X'` for `std_logic`, or a compile error for unresolved types. Give each signal exactly one driver.

## What to remember

- A process is the unit of sequential code inside an otherwise concurrent file.
- Clocked processes (`rising_edge(clk)`) synthesize to flip-flops — that's how you get memory.
- Prefer `rising_edge(clk)` over `clk'event and clk='1'`.
- List every signal a combinational process reads, or use `process(all)`; an incomplete list makes sim and synthesis disagree.
- One signal, one driver.
