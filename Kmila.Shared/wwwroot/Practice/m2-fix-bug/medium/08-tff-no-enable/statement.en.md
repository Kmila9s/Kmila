## T flip-flop — enable input ignored

A T flip-flop with **toggle enable** `t` was specified.

### Specification

* Async active-high `reset` clears `q` to `'0'`.
* On `rising_edge(clk)`:
  * If `t = '1'`, toggle: `q <- not q`.
  * If `t = '0'`, hold: `q` keeps its value.

### What's wrong

The current code toggles `q` on every rising edge regardless of `t`. The flip-flop behaves like a clock divider rather than a controllable T flip-flop.

### Hint

The toggle assignment must sit inside an `if t = '1' then ...` guard within the rising-edge branch.
