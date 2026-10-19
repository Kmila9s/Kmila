# Fix it: the tristate buffer that always drives

A tristate buffer is a switchable output. When the output-enable (`oe`) signal is high, it passes its data through to the bus. When `oe` is low, it **releases** the bus — it drives high-impedance (`'Z'`) so other devices can talk. It's the one place in this unit where the `'Z'` value from the nine-valued logic earns its keep.

## What's wrong

Open the sample and look at the body:

```vhdl
y <= d when oe = '0' else 'Z';
```

Read it out loud: "`y` equals `d` **when `oe` is 0**, otherwise `'Z'`." That's backwards. You want the buffer to pass data when the enable is **high**, and release when it's **low**.

## Your mission

- Open the sample in the editor.
- Fix the condition on the `y <=` assignment so the sense of `oe` is correct:

```vhdl
y <= d when oe = '1' else 'Z';
```

- Run the simulation and verify in the Waveforms tab:
  - When `oe = '0'`, `y` should be `'Z'`.
  - When `oe = '1'`, `y` should track `d`.

## Why `'Z'` is special here

`'Z'` isn't a third logic *level* — it means "I'm not driving." On the waveform GTKWave shows it as a line parked in the middle, neither high nor low. The whole point of a tristate is to let a wire have **several** potential drivers while only one is active at a time. That's only safe because `std_logic` is a *resolved* type: when one driver is `'Z'` and another drives `'0'` or `'1'`, resolution lets the strong value win. If every driver is `'Z'`, the wire floats.

## Extra: watch for `'X'`

If two tristate buffers drive the same bus at the same time with **different** values, a real circuit would fight itself and overheat. The resolution function models this as `'X'` (strong unknown) on the waveform — a clear red flag that your enables overlap. Build a tiny testbench with two of your buffers wired to the same `y`, turn both `oe` lines on with different `d`, and watch the bus go `'X'`.

## Why tristate matters (and where it lives today)

Tristate buffers are how multiple drivers share a single wire (a bus) without short-circuiting each other. One important caveat for the hardware you'll actually target: **FPGAs don't have internal tristate buffers** in their logic fabric — synthesis replaces an internal tristate bus with multiplexers automatically. Tristates survive only at the **I/O pads**, where they're exactly how bidirectional pins work: DDR memory data lines, I²C `sda`, and other shared buses all turn the pin driver on and off with an `oe`. That bidirectional pad is modelled with an `inout` port:

```vhdl
io_pin <= drive_value when oe = '1' else 'Z';  -- drive or release the pad
read_value <= io_pin;                           -- always sample what's there
```

## What to remember

- A tristate passes data when enabled and drives `'Z'` (released) when not — get the sense of `oe` right.
- `'Z'` means "not driving," which is what lets many drivers share one resolved `std_logic` wire.
- Two active drivers with different values resolve to `'X'` — a bug you can see on the waveform.
- Inside an FPGA, tristates become muxes; real tristates live at the I/O pads for bidirectional pins (`inout`).
