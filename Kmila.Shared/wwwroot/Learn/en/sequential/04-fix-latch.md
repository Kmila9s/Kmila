# Fix it: the accidental latch

This is one of the two or three bugs every VHDL student writes at least once. The code looks correct on paper, builds without errors, simulates with a plausible output — and when you synthesize it, the tool warns you that it inferred a **latch** you never asked for.

## The starter

```vhdl
architecture rtl of latch_bug is
begin
  process(enable, a, b)
  begin
    if enable = '1' then
      y <= a and b;
    end if;
    -- Missing: what should y be when enable = '0'?
  end process;
end architecture rtl;
```

## What goes wrong

In a purely combinational process, every output must be assigned **on every path** through the process. This one only assigns `y` when `enable = '1'`. When `enable = '0'`, the process says nothing about `y`.

VHDL's solution to "you didn't tell me what to do with this signal": **remember the previous value**. That's a latch — a transparent memory element. Synthesis will infer one, your design will have a timing-path nightmare, and the board will glitch on signal transitions.

## Your mission

Open the sample and make the design **purely combinational** — no latch, no flip-flop, just gates. Two idiomatic fixes:

**Fix 1: default assignment at the top**

```vhdl
process(enable, a, b)
begin
  y <= '0';                       -- default
  if enable = '1' then
    y <= a and b;
  end if;
end process;
```

A signal can be assigned multiple times in a process — only the last one takes effect. The default at the top means "unless something overrides me, be `'0'`".

**Fix 2: complete the `if`**

```vhdl
process(enable, a, b)
begin
  if enable = '1' then
    y <= a and b;
  else
    y <= '0';
  end if;
end process;
```

Both are right. Both produce identical hardware. Pick whichever reads better for the design you're working on.

## Verify the fix

After your fix, run the sim. In the waveform:

- For `15–25 µs` (when `enable = '1'` and `a=b='1'`): `y = '1'`.
- At every other time: `y = '0'`.

Before the fix, `y` would stay `'1'` when `enable` dropped back to `'0'` — that's the latch holding its previous value, which is exactly what you're trying to avoid.

## The rule

In a combinational process, **every output must be assigned on every path**. If you can't satisfy that, either add a default or you're actually wanting a flip-flop — in which case you should move to a clocked process and own that decision.
