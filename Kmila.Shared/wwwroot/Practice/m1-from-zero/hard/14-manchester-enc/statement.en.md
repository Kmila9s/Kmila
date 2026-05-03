## Manchester encoder

Encode a serial input `din` so that each bit period contains one transition (0→1 for `din=0`, 1→0 for `din=1`).

### Spec

- Internal `phase` toggles each clock cycle.
- Reset clears `phase` to `'0'`.
- Output: `m <= din xor phase;`
- Result: when `din=0`, `m` follows the sequence `0,1,0,1,...` → encodes as `'01'`. When `din=1`, `m` follows `1,0,1,0,...` → `'10'`.

### Hint

This is one toggling flop and one xor — the simplest line-coding scheme.
