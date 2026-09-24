## مشفِّر مانشستر

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

## Manchester encoder

Encode a serial input `din` so that each bit period contains one transition (0→1 for `din=0`, 1→0 for `din=1`).

### Spec

- Internal `phase` toggles each clock cycle.
- Reset clears `phase` to `'0'`.
- Output: `m <= din xor phase;`
- When `din=0`, `m` is `0,1,0,1,...` → `'01'`. When `din=1`, `m` is `1,0,1,0,...` → `'10'`.
