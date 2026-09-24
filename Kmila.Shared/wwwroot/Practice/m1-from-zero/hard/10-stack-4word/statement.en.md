## 4-word stack flags

Implement the occupancy tracker for a 4-word LIFO stack. Only the `full` and `empty` flags are exposed.

### Spec

- 3-bit `unsigned` counter `cnt` ranging 0..4.
- Each clock:
  - if only `push='1'` and `cnt < 4`, `cnt <= cnt + 1`.
  - if only `pop='1'` and `cnt > 0`, `cnt <= cnt - 1`.
  - otherwise hold (including when both are high — clamp).
- `empty <= '1'` when `cnt = 0`.
- `full <= '1'` when `cnt = 4`.

### Hint

The control logic is identical to a depth-4 FIFO — only the data ordering differs, and that is not modelled here.
