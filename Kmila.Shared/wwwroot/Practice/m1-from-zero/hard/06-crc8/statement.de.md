## CRC-8 seriell

Aktualisiere ein 8-Bit-CRC-Register mit einem Bit pro Takt, Polynom `0x07` (`x^8 + x^2 + x + 1`).

### Spezifikation

- Halte ein 8-Bit-Register `r`, Reset auf Null.
- Pro steigender Flanke:
  1. `fb := r(7) xor din;`
  2. Links schieben: `r(6 downto 0) & '0'`.
  3. Wenn `fb = '1'`, XOR mit dem Polynom `00000111`.
- Treibe `crc <= r` ständig.

### Hinweis

Eine `variable fb` im Prozess ist sauber und synthetisierbar.
