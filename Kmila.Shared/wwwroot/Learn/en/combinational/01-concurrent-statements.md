# Concurrent assignments: `when/else` and `with/select`

Combinational logic in VHDL is written with concurrent statements — every line outside of a process describes a piece of permanently-wired hardware. Two syntax flavours exist for the same job: **conditional** (`when/else`) and **selected** (`with/select`) assignment. These are the "sentencias concurrentes" from the HDL unit, and they're the backbone of every combinational circuit in the course: adders, code converters, comparators, multiplexers, encoders, decoders.

## `when/else` — conditional

Good when you have a prioritized list of conditions, or when each branch checks a *different* signal:

```vhdl
y <= (others => '0') when reset = '1' else
     a               when sel = "00"  else
     b               when sel = "01"  else
     c               when sel = "10"  else
     d;
```

The first matching condition wins. Ordering matters — it compiles to a **priority mux**: the topmost condition has the most direct path to `y`. Note that all the value expressions (`a`, `b`, `c`, `d`, the all-zeros aggregate) must have the **same type and width** as `y`, and the final `else` has no condition — it's the catch-all.

## `with/select` — selected

Good when you're switching on a single expression (the way a software `switch`/`case` statement does):

```vhdl
with sel select
  y <= a   when "00",
       b   when "01",
       c   when "10",
       'Z' when others;
```

All choices compare against the same expression (`sel`), the branches are separated by **commas** (not `else`), and `when others` is **mandatory** to cover every possible value. This style compiles to a **balanced mux** — no branch is privileged.

### Why `when others` is non-negotiable

`sel` here is a `std_logic_vector(1 downto 0)`. You'd think that's four cases — `"00"`, `"01"`, `"10"`, `"11"`. But each bit is a *nine-valued* `std_logic`, so the compiler counts 9² = 81 possible patterns (`"0X"`, `"ZU"`, …). You can't enumerate them, so `when others` is required to make the statement total. In synthesis the metavalues collapse away and you get the clean 4-way mux you intended.

You can also group choices with the bar operator:

```vhdl
with opcode select
  result <= a + b    when "000" | "001",   -- two codes, same output
            a - b    when "010",
            (others => '0') when others;
```

## Which one should you pick?

- **Same signal, many patterns** (decoder, mux, code converter): `with/select` reads like a truth table. Use it.
- **Different signals, priority logic** (reset beats enable beats data): `when/else`. Natural fit.
- **Synthesis output:** on a modern FPGA both produce roughly the same gates, so picking the clearer form at the source level is almost always the right call. Reach for `when/else` only when you genuinely want priority; otherwise `with/select` signals "no priority" to the next reader.

## The latch trap (preview)

Both forms require that `y` ends up assigned in **every** scenario. A concurrent `when/else` or `with/select` can't actually leave `y` unassigned — the syntax forces a final `else` / `when others`. But the equivalent **if/case inside a process** can, and *that* is where the synthesizer infers a latch to "remember" the last value, which is almost never what you want. This is the same latch trap you'll meet head-on in the sequential module's fix-it exercise, so keep the habit: assign the output on every path.

## A quick worked tie-in: a 2-bit comparator

The magnitude comparator from the course drops straight out of `when/else`:

```vhdl
-- gt/eq/lt are single std_logic outputs
gt <= '1' when unsigned(a) > unsigned(b) else '0';
eq <= '1' when a = b else '0';
lt <= '1' when unsigned(a) < unsigned(b) else '0';
```

Three independent concurrent statements, three comparators, all live at once.

## Where this appears next

You'll use `with/select` in the 3-to-8 decoder sample right after this lesson, and `when/else` throughout the adder, comparator, and mux work. Both are bread-and-butter VHDL.

## What to remember

- `when/else` = conditional, prioritized, different signals allowed → priority mux.
- `with/select` = one selector, comma-separated choices, `when others` mandatory → balanced mux.
- Every value branch must match the target's type and width.
- Concurrent forms can't drop the output, but the process-based `if`/`case` versions can — and that's where latches sneak in.
