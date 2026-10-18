# Decoder

A **decoder** turns an n-bit address into a **one-hot** output: exactly one of its 2ⁿ outputs is 1, the one named by the address.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/12-decoder.html" title="Interactive combinational block on a module bench" loading="lazy"></iframe></div>

Move the cursor (or the arrow keys) to step through every input combination. The input plungers set the bits, the module computes, the output lamps light, and the **current row of the table highlights** so you can read the function directly.

In VHDL:

```vhdl
with a select y <=
  "0001" when "00",
  "0010" when "01",
  "0100" when "10",
  "1000" when others;
```

It is purely **combinational** — the outputs depend only on the current inputs, with no clock and no memory. Decoders drive address lines: memory chips, seven-segment displays and instruction dispatch all pick one line out of many this way.
