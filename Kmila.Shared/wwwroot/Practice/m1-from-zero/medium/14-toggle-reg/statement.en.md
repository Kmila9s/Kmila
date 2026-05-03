## Toggle register with enable

Build a 1-bit toggle register. Whenever `en = '1'` on a rising clock edge, the output `q` flips. When `en = '0'`, `q` is unchanged. An asynchronous active-high `reset` clears `q` to `0`.

### Spec

- Internal: `signal qi : std_logic := '0';`.
- Process: `if en = '1' then qi <= not qi;`.
- Output: `q <= qi;`.

### Hint

Almost identical to a T flip-flop. The enable acts as the toggle gate.
