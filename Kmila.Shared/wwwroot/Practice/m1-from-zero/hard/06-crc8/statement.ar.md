## CRC-8 تسلسلي

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

## CRC-8 serial accumulator

Update an 8-bit CRC register one bit per clock using polynomial `0x07` (`x^8 + x^2 + x + 1`).

### Spec

- Hold an 8-bit register `r`, reset to all zeros.
- Each rising edge:
  1. `fb := r(7) xor din;`
  2. shift left: `r(6 downto 0) & '0'`.
  3. if `fb = '1'`, xor the shifted result with the polynomial `00000111`.
- Drive `crc <= r` continuously.

### Hint

Use a `variable fb` inside the clocked process — it is cheap, scoped, and synthesisable.
