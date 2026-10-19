# Sample: 8-bit shift register

A shift register is a row of flip-flops chained together. Each clock edge shifts the stored bits one position, and a new bit enters at one end. They're at the core of UARTs, SPI controllers, CRC engines, and LFSR-based random-number generators — the practical payoff of the flip-flops from the previous lesson.

## What this sample does

`shift8` is a **serial-in, parallel-out** (SIPO) shift register:

- `din` is the new bit arriving on each rising clock edge.
- `r` is the internal 8-bit register.
- `q` exposes `r` to the outside as a parallel byte.

"Serial in" because bits enter one at a time; "parallel out" because you can read all eight at once. That's exactly what a UART receiver does: it clocks in bits one by one off the wire, then hands the assembled byte to the rest of the system.

## The body

```vhdl
signal r : std_logic_vector(7 downto 0) := (others => '0');
...
process(clk, reset)
begin
  if reset = '1' then
    r <= (others => '0');
  elsif rising_edge(clk) then
    r <= r(6 downto 0) & din;
  end if;
end process;

q <= r;
```

The magic is on this line:

```vhdl
r <= r(6 downto 0) & din;
```

`&` is the **concatenation** operator. `r(6 downto 0)` is the lower 7 bits of the current register. We stick `din` onto the right end, making a new 8-bit value, and assign it back to `r`. Because this is a signal assignment inside a clocked process, the shift happens on every rising edge — and because signals update at the end of the step, the right-hand side uses the *old* `r`, which is exactly what a shift needs.

## It's just eight flip-flops in a chain

Don't let the one-line body hide the hardware. `r` is eight D flip-flops. The assignment wires each flip-flop's output to the next flip-flop's input, with `din` feeding the first and bit 7 falling off the end (discarded here). That chain *is* the shift register — the concatenation is only a compact way to describe the wiring.

## Waveform walk-through

The sim config drives `din` through a pattern: `1, 0, 1, 1, 0, 1, 0, 1`. Watch `q` over time:

- After the first tick: `q = 00000001`
- After the second: `q = 00000010`
- After the third: `q = 00000101`
- …and so on. The new bit lands in the LSB; older bits march toward the MSB.

If you zoom out you'll see the first `din` value reach the top bit (bit 7) exactly 8 clock cycles after it arrived. That's the round-trip latency of an 8-bit shift register — and the reason a serial link has a fixed, predictable delay.

## A pitfall: shifting the wrong way

Beginners often get the direction backwards and lose a bit. If you write `r <= r(7 downto 1) & din;` you've kept the *top* seven bits and dropped bit 0 — that's a shift *right*, not left. Decide which end new data enters and which end falls off, then make the slice match. The two correct forms are:

```vhdl
r <= r(6 downto 0) & din;   -- shift LEFT: din -> LSB, MSB discarded
r <= din & r(7 downto 1);   -- shift RIGHT: din -> MSB, LSB discarded
```

## Your turn

- **Shift right instead.** Use the second form above and confirm the walking bit now moves the other way.
- **Parallel load (PISO/universal).** Add a `load` input and a `din_par` bus. When `load = '1'` on a clock edge, `r <= din_par` (take the whole bus at once); otherwise shift. That's the heart of a *parallel-in, serial-out* transmitter.
- **Serial output.** Add `dout <= r(7);`. Now it's serial-in, serial-out (SISO) — the first bit you sent on `din` appears on `dout` eight cycles later, the delay a UART RX relies on.
- **Make an LFSR.** Feed `r(7) xor r(5) xor r(4) xor r(3)` back into the input instead of `din`. You now have a pseudo-random sequence generator — the same structure used for CRCs and test patterns.

## What to remember

- A shift register is a chain of D flip-flops; each edge moves every bit one place and admits a new bit at one end.
- `r <= r(6 downto 0) & din;` shifts left (new bit in the LSB); swap the slice and order to shift right.
- Adding a parallel load turns it into a SIPO/PISO converter — the core of serial links like UART and SPI.
- Feed selected taps back through XOR and it becomes an LFSR for CRCs and pseudo-random sequences.
