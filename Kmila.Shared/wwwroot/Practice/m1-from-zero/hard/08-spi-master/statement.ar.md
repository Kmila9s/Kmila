## سيد SPI

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

## SPI master shift

Implement a minimal SPI master that shifts an 8-bit byte MSB-first when `start='1'`.

### Spec

- A 4-bit phase counter `phase` paces the operation; once active it increments every clock.
- `sclk = phase(0)` while active.
- `mosi = shreg(7)` while active — i.e. the current MSB.
- Shift `shreg` left whenever `phase(0) = '1'`.
- Reset clears everything.

### Hint

A small `active` flag keeps the outputs at `'0'` while idle.
