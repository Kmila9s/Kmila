## Rising-edge detector

Output a one-clock-wide pulse on `pulse` whenever `d` transitions from `0` to `1`. An asynchronous active-high `reset` clears the internal state.

### Spec

- Internal: `signal d_prev : std_logic := '0';`.
- Each rising edge: `d_prev <= d;`.
- Combinational output: `pulse = '1'` when `d = '1' and d_prev = '0'`.

### Hint

Compare the current input `d` with its sampled previous value to detect the rising edge.
