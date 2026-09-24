## إشارات مكدّس 4 كلمات

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

## 4-word stack flags

Implement the occupancy tracker for a 4-word LIFO stack. Only the `full` and `empty` flags are exposed.

### Spec

- 3-bit `unsigned` counter `cnt` ranging 0..4.
- Each clock:
  - if only `push='1'` and `cnt < 4`, `cnt <= cnt + 1`.
  - if only `pop='1'` and `cnt > 0`, `cnt <= cnt - 1`.
  - otherwise hold.
- `empty <= '1'` when `cnt = 0`.
- `full <= '1'` when `cnt = 4`.
