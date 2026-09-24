# Sample: 3-to-8 decoder

A decoder takes a small binary code and turns it into a **one-hot** output: exactly one output is high, and its position matches the code.

```
  sel  │  y
  000  │  00000001
  001  │  00000010
  010  │  00000100
  011  │  00001000
  100  │  00010000
  101  │  00100000
  110  │  01000000
  111  │  10000000
```

Decoders live inside every memory system — the address lines drive a decoder, and the "row match" output enables exactly one row. Whenever you hear "address decoder", that's what's underneath.

## The VHDL

A decoder is a textbook case for `with/select`:

```vhdl
with sel select
  y <=
    "00000001" when "000",
    "00000010" when "001",
    "00000100" when "010",
    "00001000" when "011",
    "00010000" when "100",
    "00100000" when "101",
    "01000000" when "110",
    "10000000" when others;
```

Notice `when others` at the end. `sel` is a `std_logic_vector(2 downto 0)` — 3 bits — so there are 2³ = 8 binary values, but because `std_logic` has nine states, the compiler needs the `others` clause to cover every combination (it'll synthesize to the same thing as `"111"`).

## What the sample does

The sample cycles `sel` through all eight 3-bit values, 10 µs per value. On the Waveforms tab you'll see the 8 bits of `y` turn into a walking `'1'` — a digital "light chaser".

## Your turn

Once it runs:

- Widen the decoder to a 4-to-16 version. You can do it by hand, or you can rewrite the body as `y <= std_logic_vector(shift_left(to_unsigned(1, 16), to_integer(unsigned(sel))));`. Both synthesize identically — pick the one you find more readable.
- Add an `enable` input. When `enable = '0'`, all outputs are low regardless of `sel`.
