## {{PROJECT_NAME}} — Moore FSM scaffold

A Moore finite-state machine in the canonical two-process style:

- A **state register** process — synchronous, only depends on `clk` / `reset`.
- A **next-state** process — combinational, depends only on the current state and the inputs.
- A single Moore output assignment — depends only on the current state.

## States

| State    | Meaning                                  |
| -------- | ---------------------------------------- |
| `S_IDLE` | Waiting for `trigger` to go high.        |
| `S_WORK` | One-cycle work pass.                     |
| `S_DONE` | Pulses `done` high for one cycle.        |

## Where to edit

1. Add or rename states in the `state_t` enum.
2. Update the transitions inside `next_logic`'s `case` block.
3. Adjust the Moore output at the bottom (or add more outputs).

## Layout

```
main.vhd               -- the FSM
tb/main_tb.vhd         -- testbench that exercises one trigger pulse
docs/README.md         -- this file
```
