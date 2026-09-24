## Divide-by-4 clock

Generate an output clock at one quarter of the input clock frequency. The simplest method is to count rising edges and output the high bit of a 2-bit counter — it toggles every 4 input cycles.

### Spec

- Internal: `signal cnt : unsigned(1 downto 0) := (others => '0');`.
- On each rising edge: `cnt <= cnt + 1;`.
- Output: `clk_out <= cnt(1);`.

### Hint

`cnt(1)` toggles after the counter increments through `00 → 01 → 10 → 11`, giving you `clk/4`.
