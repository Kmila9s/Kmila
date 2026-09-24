## Edge detector — wrong edge

A **rising-edge** detector for `d` was specified.

### Specification

* Sample `d` once per `rising_edge(clk)` into `d_prev`.
* `pulse = '1'` for one cycle when `d` was `'0'` last cycle and is `'1'` now.
* Async active-high `reset` clears `d_prev`.

### What's wrong

The combinational expression for `pulse` checks the wrong polarity: it fires when `d` falls from `'1'` to `'0'`, i.e. on the **falling** edge. So when `d` rises (the spec target), `pulse` stays `'0'`, and when `d` falls it pulses by mistake.

### Hint

Swap the two equality tests in the conditional assignment for `pulse`.
