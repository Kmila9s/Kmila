## {{PROJECT_NAME}} — Generic counter

A synchronous counter with a `generic WIDTH` so the same module covers
any width without duplicating code. Ships with a 4-bit testbench so you
see a full wrap quickly; bump `WIDTH` for wider counters.

## Ports

| Port     | Direction | Notes                                          |
| -------- | --------- | ---------------------------------------------- |
| `clk`    | in        | Rising-edge triggered.                         |
| `reset`  | in        | Synchronous, active high. Clears `count` to 0. |
| `enable` | in        | When low, the counter holds its value.         |
| `count`  | out       | Current count, `WIDTH` bits wide.              |
| `carry`  | out       | One-cycle pulse on wrap from all-ones to 0.    |

## Layout

```
main.vhd               -- the counter
tb/main_tb.vhd         -- 4-bit instance with full-wrap stimulus
docs/README.md         -- this file
```

## Extending

- Set a different `generic map (WIDTH => N)` in the testbench to size the counter.
- Wire `carry` into a second counter's `enable` for a ripple-style multi-width counter.
