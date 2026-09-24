## T flip-flop

Implement a toggle (T) flip-flop. On a rising clock edge, if `t = '1'`, the output `q` flips. If `t = '0'`, `q` stays. An asynchronous active-high `reset` clears `q` to `0`.

### Spec

- Internal: `signal qi : std_logic := '0';`.
- Process body: `if t = '1' then qi <= not qi;`.
- Output: `q <= qi;`.

### Hint

Drive `q` from an internal `qi` because VHDL outputs cannot be read inside a process.
