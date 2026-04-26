# Concurrent assignments: `when/else` and `with/select`

Combinational logic in VHDL is written with concurrent statements — every line outside of a process describes a piece of permanently-wired hardware. Two syntax flavours exist for the same job: **conditional** and **selected** assignment.

## `when/else` — conditional

Good when you have a prioritized list of conditions, or when each branch checks a different signal:

```vhdl
y <= '0' when reset = '1' else
     a   when mode  = "read" else
     b   when mode  = "write" else
     'Z';
```

The first matching condition wins. Ordering matters — it compiles to a **priority mux**: the topmost condition has the most direct path to `y`.

## `with/select` — selected

Good when you're switching on a single expression (the same way a software `switch` statement does):

```vhdl
with mode select
  y <=
    a   when "00",
    b   when "01",
    c   when "10",
    'Z' when others;
```

All cases compare against the same signal (`mode`), and `when others` is mandatory to cover every possible pattern. This style compiles to a **balanced mux** — no branch is privileged.

## Which one should you pick?

- **Same signal, many patterns:** `with/select` reads like a truth table. Use it.
- **Different signals, priority logic:** `when/else`. Natural fit.
- **Synthesis output:** both produce roughly the same gates on a modern FPGA — picking the clearer form at the source level is almost always the right call.

## A small trap

Both forms require that `y` ends up assigned in **every** scenario. If you forget the final `else` on a `when/else`, or skip `when others` on a `with/select`, the compiler will warn you — and the synthesizer may infer a latch to "remember" the last value, which is almost never what you want. This is the same latch trap you'll see again in the sequential module's fix-it exercise.

## Where this appears next

You'll use `with/select` in the 3-to-8 decoder sample right after this lesson, and `when/else` all over the traffic-light FSM later on. Both are bread-and-butter VHDL.
