## D flip-flop — async reset that isn't async

A D flip-flop with an **asynchronous active-high reset** was specified.

### Specification

* On `rising_edge(clk)`: `q <- d`.
* When `reset = '1'`: `q` is forced to `'0'` immediately, no clock needed.

### What's wrong

If you assert `reset` between clock edges, `q` does not clear right away — the simulator only re-evaluates the process when `clk` next changes. Effectively the reset has become **synchronous**, which violates the spec.

### Hint

Sensitivity lists drive when a process re-evaluates. An async reset must be listed alongside `clk`.
