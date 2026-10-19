# Fix it: the accidental latch

This is one of the two or three bugs every VHDL student writes at least once. The code looks correct on paper, builds without errors, simulates with a plausible output — and when you synthesize it, the tool warns you that it inferred a **latch** you never asked for.

The irony is that Unit IV teaches you to build latches (SR, D) *on purpose*. The skill here is recognising when you've built one **by accident**, inside what was supposed to be pure combinational logic.

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

VHDL's answer to "you didn't tell me what to do with this signal" is: **keep the previous value**. The only hardware that can keep a previous value is a memory element — so synthesis infers a **transparent latch**. That's exactly a D latch: while `enable = '1'` the output follows `a and b`; when `enable` drops, the last value is frozen. You asked for a gate and got a latch with an enable.

## Why an accidental latch is bad

It's not that latches are evil — it's that an *unintended* one wrecks your design:

- **Timing is hard to close.** Static timing analysis struggles with transparent latches; most FPGA flows assume edge-triggered flip-flops and treat inferred latches as a warning for good reason.
- **Glitches pass through.** While transparent, any glitch on `a` or `b` flows straight to `y` and can get latched.
- **Sim/synthesis mismatch risk.** The behaviour depends on subtle assignment coverage that's easy to get wrong.

So when the tool says "inferred latch for signal `y`," treat it as an error to fix, not a warning to ignore.

## Your mission

Open the sample and make the design **purely combinational** — no latch, no flip-flop, just gates. Two idiomatic fixes:

**Fix 1: default assignment at the top**

```vhdl
process(enable, a, b)
begin
  y <= '0';                       -- default: covers every path
  if enable = '1' then
    y <= a and b;
  end if;
end process;
```

A signal can be assigned several times in one process pass — only the last one that executes takes effect. The default at the top means "unless something below overrides me, be `'0'`." This is the scalable habit: set defaults first, then override.

**Fix 2: complete the `if`**

```vhdl
process(enable, a, b)
begin
  if enable = '1' then
    y <= a and b;
  else
    y <= '0';                     -- the missing branch
  end if;
end process;
```

Both are right. Both produce identical hardware (a 2-input AND gated by `enable`, output `'0'` when disabled). Pick whichever reads better.

## The same trap in a `case`

Latches aren't just an `if` problem. An incomplete `case`, or one branch that forgets an output, infers a latch too:

```vhdl
-- BUG: 'y' is unassigned for sel = "10" and "11"
case sel is
  when "00" => y <= a;
  when "01" => y <= b;
  when others => null;   -- null assigns nothing -> latch on y
end case;
```

Same cure: give `y` a default before the `case`, or assign it in `when others` (and in every branch). `process(all)` helps you avoid the *sensitivity-list* mistake, but it does **not** save you from the coverage mistake — you still have to assign every output on every path.

## Verify the fix

After your fix, run the sim. In the waveform:

- For `15–25 µs` (when `enable = '1'` and `a = b = '1'`): `y = '1'`.
- At every other time: `y = '0'`.

Before the fix, `y` would stay `'1'` when `enable` dropped back to `'0'` — that's the latch holding its previous value, exactly what you're trying to avoid.

## What to remember

- In a combinational process, **every output must be assigned on every path** — an `if`/`case` that misses a path infers a transparent latch.
- A default assignment at the top of the process is the easiest way to guarantee coverage.
- An *accidental* latch causes timing and glitch problems; an *intentional* one (D/SR latch) is a deliberate Unit IV design choice.
- `process(all)` fixes sensitivity lists, not assignment coverage — you still have to assign every output.
- If you really want memory, don't let it sneak in — move to a clocked process and own the decision.
