# Sample: 3-to-8 decoder

A decoder takes a small binary code and turns it into a **one-hot** output: exactly one output line is high, and its position matches the code. It's one of the named circuits in the combinational unit ("codificador y decodificador"), and it's the natural partner of the encoder, which does the reverse.

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

Decoders live inside every memory system — the address lines drive a decoder, and the "row match" output enables exactly one word line. Whenever you hear "address decoder", that's what's underneath. The same structure also implements a full set of minterms: an n-input decoder outputs all 2ⁿ minterms of its inputs, so any Boolean function can be built by OR-ing the right decoder outputs.

## The VHDL

A decoder is a textbook case for `with/select`:

```vhdl
with sel select
  y <= "00000001" when "000",
       "00000010" when "001",
       "00000100" when "010",
       "00001000" when "011",
       "00010000" when "100",
       "00100000" when "101",
       "01000000" when "110",
       "10000000" when others;
```

Notice `when others` at the end. `sel` is a `std_logic_vector(2 downto 0)` — 3 bits — so there are 2³ = 8 *intended* values, but because each bit is a nine-valued `std_logic` the compiler sees far more possible patterns and needs `others` to be exhaustive. Here `others` folds in with the `"111"` case (it synthesizes to the same output).

## The scalable alternative

Writing out eight rows is fine for a 3-to-8 decoder, but it doesn't scale. The same function in one line, using a shift, is:

```vhdl
y <= std_logic_vector(shift_left(to_unsigned(1, 8), to_integer(unsigned(sel))));
```

Read it inside-out: take `sel` as a number, shift a single `'1'` left by that many positions inside an 8-bit field, and cast back to `std_logic_vector`. Both versions synthesize identically — pick the one you find more readable. (The explicit table is easier for a beginner to verify against the truth table; the shift form is what you'd widen to 32 or 64 outputs.)

## What the sample does

The sample cycles `sel` through all eight 3-bit values, 10 µs per value. On the Waveforms tab you'll see the 8 bits of `y` turn into a walking `'1'` — a digital "light chaser".

## Add an enable (and meet the demux)

Real decoders have an **enable**. When disabled, all outputs are low:

```vhdl
y <= (others => '0') when enable = '0' else
     std_logic_vector(shift_left(to_unsigned(1, 8), to_integer(unsigned(sel))));
```

Here's a nice connection: a decoder-with-enable, where the enable is a *data* line instead of a constant, **is a demultiplexer** — the enable value is routed to the one output the address picks, and the rest stay at `'0'`. Decoder and demux are the same hardware wearing different labels.

## Your turn

Once it runs:

- Widen it to a **4-to-16** decoder. Try it by hand (sixteen rows) and then with the shift form — notice how little the shift form has to change (just the two `16`s).
- Add the `enable` above and confirm on the waveform that every output drops to `'0'` while `enable = '0'`.
- Build the inverse: an **8-to-3 priority encoder** that outputs the index of the highest set input. That's the other half of this section of the course.

## What to remember

- A decoder maps an n-bit code to a one-hot 2ⁿ output; its outputs are the minterms of the inputs.
- `with/select` with a mandatory `when others` is the clear, truth-table-shaped way to write it.
- The `shift_left(to_unsigned(1, N), …)` idiom is the version that scales to any width.
- A decoder with a data-driven enable is a demultiplexer; its mirror image is the encoder.
