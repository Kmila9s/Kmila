# Processes and sensitivity lists

Everything in VHDL outside of a process is concurrent — wires and gates. The moment you want a circuit to **remember** something from one clock cycle to the next, you need a process.

## What a process is

A `process` is a block of sequential code inside a VHDL architecture. Its lines are read top to bottom, just like software. But the process itself only "runs" when something in its **sensitivity list** changes.

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

This block means: "whenever `clk` or `reset` changes, re-evaluate my body."

## Two shapes you'll see 99% of the time

### Combinational process

Sensitivity list = every input read inside. No clock. Output is a pure function of the inputs.

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

Synthesizes to a 2-to-1 mux. No storage.

### Clocked (sequential) process

Sensitivity list = `(clk, reset)`. Assignments happen only inside `rising_edge(clk)` (or `falling_edge`, if you're unlucky). Output is **registered** — it changes only on clock edges.

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

This is the classic D flip-flop pattern.

## The sensitivity-list trap

If you forget to list a signal that the body reads, simulation and synthesis disagree:

- **Simulation**: the process won't re-evaluate when that signal changes. Waveform looks wrong.
- **Synthesis**: the tool reads the body, sees every read, and builds the right hardware anyway.

Result: your testbench passes, but the board doesn't work — or vice versa. Modern VHDL-2008 lets you write `process(all)` to avoid this entirely:

```vhdl
process(all)  -- include every signal read in the body
begin
  ...
end process;
```

Kmila's parser accepts `process(all)`, and it's what most teams use in new designs.

## A rule of thumb

- Sensitivity = just `(clk, reset)` → you're describing **registers** (flip-flops).
- Sensitivity = every input you read → you're describing **combinational logic**.
- Sensitivity mixes clocked + non-clocked signals → you've almost certainly got a bug.

## What to remember

- A process is the unit of sequential code inside an otherwise concurrent file.
- Clocked processes (`rising_edge(clk)`) synthesize to flip-flops. That's how you get memory.
- List every signal you read, or use `process(all)` to let the tool do it for you.
