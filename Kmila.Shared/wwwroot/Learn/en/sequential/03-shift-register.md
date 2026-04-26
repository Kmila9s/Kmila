# Sample: 8-bit shift register

A shift register is a row of flip-flops chained together. Each clock edge shifts the stored bits one position, and a new bit enters at one end. They're at the core of UARTs, SPI controllers, CRC engines, and LFSR-based random-number generators.

## What this sample does

`shift8` is a **serial-in, parallel-out** shift register:

- `din` is the new bit arriving on each rising clock edge.
- `r` is the internal 8-bit register.
- `q` exposes `r` to the outside as a parallel byte.

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

`&` is the **concatenation** operator. `r(6 downto 0)` is the lower 7 bits of the current register. We stick `din` onto the right end, making a new 8-bit value, and assign it back to `r`.

Because this is a signal assignment inside a clocked process, the shift happens on every rising edge.

## Waveform walk-through

The sim config drives `din` through a pattern: `1, 0, 1, 1, 0, 1, 0, 1`. Watch `q` over time:

- After the first tick: `q = 00000001`
- After the second: `q = 00000010`
- After the third: `q = 00000101`
- …and so on. The new bit lands in the LSB; older bits march toward the MSB.

If you zoom out you'll see the top bit (bit 7) getting the first `din` value exactly 8 clock cycles after it arrived. That's the round-trip latency of an 8-bit shift register.

## Your turn

- **Shift right instead.** Swap the concatenation: `r <= din & r(7 downto 1);`.
- **Parallel load.** Add a `load` input and a `din_parallel` bus. When `load = '1'` on a clock edge, the register takes the whole bus at once instead of shifting.
- **Serial output.** Add `dout <= r(7);`. Now it's a serial-in, serial-out (SISO) shift register — the first bit you sent on `din` shows up on `dout` eight cycles later. This is how the bit-for-bit delay in UART RX works.
