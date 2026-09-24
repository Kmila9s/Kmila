## UART mid-bit sampler — fill the gap

In a 16× over-sampled UART receiver, the bit decision happens
**at the centre of the bit window** — that is, when the
oversample counter reaches 8. The clock-edge detection,
asynchronous reset and output port `sample_now` are already
wired for you.

Your task is to write the comparator that produces the
**sample-now strobe** from the running `bit_count`.

### Specification

* `bit_count` is `STD_LOGIC_VECTOR(3 downto 0)` carrying the 0..15
  oversample index.
* Drive `sample_now <= '1'` exactly when `bit_count = "1000"` (8).
* Drive `sample_now <= '0'` for any other value.
* The block is inside the `rising_edge(clk)` branch, so each
  decision shows up one cycle later.

### Hint

```vhdl
if unsigned(bit_count) = "1000" then
    sample_now <= '1';
else
    sample_now <= '0';
end if;
```

### Where to write

Only the lines between `-- BEGIN_EDIT` and `-- END_EDIT` are
graded.
