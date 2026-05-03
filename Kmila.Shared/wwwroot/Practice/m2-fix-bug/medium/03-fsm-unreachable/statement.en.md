## 3-state FSM — unreachable state

A 3-state machine with states `S0` (`"00"`), `S1` (`"01"`), `S2` (`"10"`) was specified.

### Specification

* Async active-high reset → `S0`.
* On `rising_edge(clk)`:
  * `S0 -> S1` always.
  * `S1 -> S2` when `x = '1'`, otherwise stays in `S1`.
  * `S2 -> S0` always.
* `state` outputs the current state encoding.

### What's wrong

The transition `S1 -> S2` never fires. No matter how long `x` is held high, the FSM stays in `S1` forever — `S2` is **unreachable**. The bug is the condition guarding that transition.

### Hint

A signal cannot simultaneously equal two different values. Look at the boolean expression — it's a contradiction.
