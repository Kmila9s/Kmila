# Sample: your first entity (2-to-1 MUX)

A **multiplexer** is the simplest combinational device that has a "choice" in it. The 2-to-1 version selects between two inputs based on one `sel` bit:

```
  sel = 0  →  y = a
  sel = 1  →  y = b
```

The VHDL for that is one line:

```vhdl
y <= a when sel = '0' else b;
```

## What the sample does

Open the sample and you'll get a project named **Your first entity** with:

- `mux21.vhd` (the design above)
- A companion simulation config that drives `sel`, `a`, and `b` over 40 microseconds

When you Run, the Waveforms tab shows four rows — the three inputs and the output. Watch `y` flip between `a` and `b` as `sel` toggles.

## Things to notice

The assignment `y <= a when sel = '0' else b;` is a **concurrent statement**: it lives outside of any process, and it describes a permanent wiring. There's no "when does this line execute?" — it just is.

The condition inside a `when/else` can be any boolean expression:

```vhdl
y <= a when sel = '0' else
     b when enable = '1' else
     'Z';
```

Chains like that synthesize to a priority multiplexer (top case wins ties).

## Your turn: extend it

Once you've seen the sample work:

1. Change the entity to a **4-to-1 MUX** — `sel` becomes 2 bits, and you choose between `a`, `b`, `c`, `d`.
2. Add a fifth port `output_enable`. When it's `'0'`, drive `y <= 'Z'` (high impedance) regardless of `sel`.
3. Compare `when/else` against the `with/select` version you'll meet in the next module.

Kmila's editor highlights the VHDL keywords and knows about `std_logic` — hover over `when` to see the built-in documentation.
