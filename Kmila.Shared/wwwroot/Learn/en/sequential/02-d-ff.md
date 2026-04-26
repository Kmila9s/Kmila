# Sample: D flip-flop with async reset

The D flip-flop (DFF) is the most common memory cell in digital design. A modern FPGA has tens to hundreds of thousands of them. Every register, counter, and pipeline stage is built out of DFFs.

## What it does

On every rising edge of `clk`, the value on `d` is copied to `q`. Between edges, `q` holds its value even if `d` changes.

When `reset` is high, `q` is forced to `'0'` immediately — this is the **asynchronous** reset.

## The code

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= '0';
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

Line by line:

- **Sensitivity list `(clk, reset)`** — the process wakes up on either. That's what makes the reset async.
- **`if reset = '1'`** — reset has priority. If the reset pin is high, we don't care what the clock or `d` are doing.
- **`elsif rising_edge(clk)`** — only when the clock goes from 0 to 1 does `d` get sampled.

## Waveform walk-through

The sample drives `reset = '1'` for the first 5 µs, then releases it. `d` toggles at 10, 30, and 50 µs. Switch to the Waveforms tab after Run and notice:

- Before 5 µs, `q = '0'` (held by reset).
- After reset releases, `q` only changes on clock rising edges — the value of `d` at the edge gets captured.
- Changes to `d` between clock edges have no effect.

## Sync vs async reset

There are two schools of thought:

**Async reset** (`if reset = '1'` outside the edge block, as above):
- Pros: resets even if the clock isn't running. Handles power-on cleanly.
- Cons: harder to meet timing at very high clock speeds; Xilinx's recommendation for 7-series and later is to avoid it.

**Sync reset** (`if rising_edge(clk) then if reset = '1'…`):
- Pros: behaves like any other input on the clock edge; plays nicely with the synthesizer.
- Cons: needs the clock to be running to reset.

For learning, async reset is easier to reason about. For production FPGA work, read your vendor's guidance.

## Your turn

- Add an **enable** input. `q` should only update on the rising edge when `enable = '1'`.
- Make it **register a 4-bit bus** instead of a single bit. Change `d` and `q` to `std_logic_vector(3 downto 0)` and adjust the reset value to `(others => '0')`.
- Add a `q_not` output that's always the inverse of `q`. Bonus: wire `q_not` back into `d` and watch it become a divide-by-two clock.
