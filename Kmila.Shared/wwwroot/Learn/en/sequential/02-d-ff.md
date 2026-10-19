# Sample: D flip-flop with async reset

The D flip-flop (DFF) is the most common memory cell in digital design — the star of Unit IV's "elementos de memoria tipo Flip-Flop". A modern FPGA has tens to hundreds of thousands of them. Every register, counter, and pipeline stage is built out of DFFs.

## Flip-flop vs. latch (the one distinction to nail)

Both store a bit, but they listen differently:

- A **latch** is *level-sensitive*: while its enable is high, the output follows the input (it's "transparent"); it holds only when the enable is low.
- A **flip-flop** is *edge-sensitive*: it samples the input at one instant — the clock edge — and holds for the rest of the cycle.

This lesson builds a flip-flop. The next fix-it exercise shows what happens when you *accidentally* build a latch. Keep the difference in mind: edge-triggered = flip-flop, level-triggered = latch.

## What it does

On every rising edge of `clk`, the value on `d` is copied to `q`. Between edges, `q` holds its value even if `d` changes. When `reset` is high, `q` is forced to `'0'` immediately — this is the **asynchronous** reset.

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

- **Sensitivity list `(clk, reset)`** — the process wakes on either. Listing `reset` is what makes the reset asynchronous: it acts the moment `reset` rises, not at the next clock edge.
- **`if reset = '1'`** — reset has priority. If the reset pin is high, we don't care what the clock or `d` are doing.
- **`elsif rising_edge(clk)`** — only when the clock goes from 0 to 1 does `d` get sampled into `q`.

## Setup and hold: why edges need breathing room

A flip-flop can only capture `d` reliably if `d` is stable for a short window **before** the edge (setup time) and **after** it (hold time). Violate that window and the output can go *metastable* — hovering between `'0'` and `'1'` before settling unpredictably. You don't model these times in this sample, but they're the reason real designs register asynchronous inputs through two flip-flops (a "synchronizer") before using them. Good to know the name now; you'll meet it again.

## Waveform walk-through

The sample drives `reset = '1'` for the first 5 µs, then releases it. `d` toggles at 10, 30, and 50 µs. Switch to the Waveforms tab after Run and notice:

- Before 5 µs, `q = '0'` (held by reset).
- After reset releases, `q` only changes on clock rising edges — the value of `d` *at the edge* gets captured.
- Changes to `d` between clock edges have no effect on `q`.

## Sync vs. async reset

There are two schools, and the choice has real consequences:

**Async reset** (`if reset = '1'` outside the edge block, as above):
- Pros: resets even if the clock isn't running; handles power-on cleanly.
- Cons: the *release* of the reset is not aligned to the clock, so different flip-flops can leave reset on different edges — a metastability risk — and it can limit the maximum clock frequency.

**Sync reset** (`if rising_edge(clk) then if reset = '1' then …`):
- Pros: behaves like any other synchronous input; maps directly onto the flip-flop's built-in reset and plays nicely with timing closure.
- Cons: needs the clock running to take effect.

For *learning*, async reset is easier to reason about, which is why this sample uses it. For *production FPGA work*, vendor guidance (for example AMD/Xilinx's "Get Smart About Reset" white paper and later methodology guides) generally favours **synchronous** resets, and some free-running logic needs no reset at all. Follow your target device's recommendation.

## The other flip-flops

The course also covers SR, JK, and T flip-flops. They're all a D flip-flop with a little combinational logic on the front that computes the next state:

- **T (toggle):** `d = q xor t` → holds when `t=0`, flips when `t=1`.
- **JK:** `d = (j and not q) or (not k and q)`.
- **SR:** set/reset, with the `s=r=1` case defined away.

So once you can write a clean D flip-flop, you can build any of them.

## Your turn

- Add an **enable** input: `q` should update on the rising edge only when `enable = '1'`.
- Make it **register a 4-bit bus**: change `d` and `q` to `std_logic_vector(3 downto 0)` and the reset value to `(others => '0')`.
- Add a `q_not` output that's always `not q`. Bonus: wire `q_not` back into `d` and watch it become a **divide-by-two** clock — a T flip-flop in disguise.
- Convert the reset to **synchronous** and compare the waveforms at reset release.

## What to remember

- A D flip-flop samples `d` on the clock edge and holds between edges — edge-triggered, unlike a level-sensitive latch.
- `process(clk, reset)` with the reset tested *before* `rising_edge` gives an asynchronous reset; put it *inside* the edge for a synchronous one.
- Respect setup/hold; synchronize asynchronous inputs to avoid metastability.
- SR, JK, and T flip-flops are just a D flip-flop plus next-state logic.
