# Fix it: the tristate buffer that always drives

A tristate buffer is a switchable output. When the output-enable (`oe`) signal is high, it passes its data through to the bus. When `oe` is low, it **releases** the bus — it drives high-impedance (`'Z'`) so other devices can talk.

## What's wrong

Open the sample and look at the body:

```vhdl
y <= d when oe = '0' else 'Z';
```

Read it out loud: "`y` equals `d` **when `oe` is 0**, otherwise `'Z'`." That's backwards. You want the buffer to pass data when the enable is **high**, and release when it's **low**.

## Your mission

- Open the sample in the editor.
- Fix the condition on line 12 (the `y <=` assignment).
- Run the simulation and verify in the Waveforms tab:
  - When `oe = '0'`, `y` should be `'Z'`.
  - When `oe = '1'`, `y` should track `d`.

## Why tristate matters

Tristate buffers are how multiple drivers share a single wire (a bus) without short-circuiting each other. FPGAs don't have tristates in their internal fabric — they use multiplexers instead — but **external** tristates (at the I/O pads) are still how DDR memory and PCIe endpoints share pins.

## Extra: watch for `'X'`

If two tristate buffers both drive the same bus at the same time with different values, a real circuit would smoke. The simulator shows this as `'X'` (unknown) on the waveform. Try building a tiny testbench with two of your buffers wired to the same `y` and watch what happens.
