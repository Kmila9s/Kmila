## FIFO empty/full flags

Implement the count tracker for a depth-4 FIFO. You only expose the `full` and `empty` flags — the data path itself is not modelled.

### Spec

- 3-bit `unsigned` counter `cnt` ranging 0..4.
- Each clock:
  - if only `push='1'` and `cnt < 4`, `cnt <= cnt + 1`.
  - if only `pop='1'` and `cnt > 0`, `cnt <= cnt - 1`.
  - otherwise hold.
- `empty <= '1'` when `cnt = 0`.
- `full <= '1'` when `cnt = 4`.

### Hint

Use `to_unsigned(4, 3)` to compare against the depth and clamp at the limits.
