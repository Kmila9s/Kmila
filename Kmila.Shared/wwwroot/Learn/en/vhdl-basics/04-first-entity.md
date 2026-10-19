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

## The whole file

So you can see all four sections from the previous lessons in one place:

```vhdl
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity mux21 is
  port (
    a   : in  std_logic;
    b   : in  std_logic;
    sel : in  std_logic;
    y   : out std_logic
  );
end entity mux21;

architecture rtl of mux21 is
begin
  y <= a when sel = '0' else b;
end architecture rtl;
```

## What the sample does

Open the sample and you'll get a project named **Your first entity** with:

- `mux21.vhd` (the design above)
- A companion simulation config that drives `sel`, `a`, and `b` over 40 microseconds

When you Run, the Waveforms tab shows four rows — the three inputs and the output. Watch `y` flip between `a` and `b` as `sel` toggles.

## Things to notice

The assignment `y <= a when sel = '0' else b;` is a **concurrent statement**: it lives outside of any process, and it describes a permanent wiring. There's no "when does this line execute?" — it just *is*. If `a`, `b`, or `sel` changes, `y` re-settles after a propagation delay.

The condition inside a `when/else` can be any boolean expression, and you can chain them:

```vhdl
y <= a   when sel = '0' else
     b   when enable = '1' else
     'Z';
```

Chains like that synthesize to a **priority** multiplexer (the topmost condition wins ties).

## The theory underneath: Shannon's expansion

That one line isn't just convenient syntax — it's the hardware form of a theorem from Boolean algebra that the course calls the **teorema de expansión de Shannon**. Any function of a variable `s` can be split as:

```
f(s, …) = (s' · f(0, …)) + (s · f(1, …))
```

A 2-to-1 mux computes exactly `y = sel'·a + sel·b`. In other words, a multiplexer is the physical realization of Shannon expansion around the select bit — which is why larger functions are so often built by stacking muxes. Keep this in your back pocket; it's the idea behind the multiplexer/demultiplexer unit later in the course.

## Your turn: extend it

Once you've seen the sample work:

1. Change the entity to a **4-to-1 MUX** — `sel` becomes a 2-bit `std_logic_vector`, and you choose between `a`, `b`, `c`, `d`. (A `with sel select` statement reads cleanly here — you'll meet it in the combinational module.)
2. Add a fifth port `output_enable`. When it's `'0'`, drive `y <= 'Z'` (high impedance) regardless of `sel`.
3. Build a **demultiplexer** — the mirror image. One input, `sel`, and several outputs; the input is routed to exactly one output while the others sit at `'0'`.

Kmila's editor highlights the VHDL keywords and knows about `std_logic` — hover over `when` to see the built-in documentation.

## What to remember

- An **entity** names the interface; the **architecture** gives the behaviour. The mux is a complete, minimal example of both.
- `y <= a when sel = '0' else b;` is a concurrent conditional assignment — permanent wiring, not a line that "runs".
- A multiplexer is Shannon's expansion in silicon: `y = sel'·a + sel·b`. That one idea scales up to every wider mux you'll build.
