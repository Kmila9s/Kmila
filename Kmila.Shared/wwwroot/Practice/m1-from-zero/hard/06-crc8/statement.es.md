## Acumulador CRC-8 serial

Actualiza un registro CRC de 8 bits, un bit por reloj, con el polinomio `0x07` (`x^8 + x^2 + x + 1`).

### Especificación

- Mantén un registro `r` de 8 bits, reset a ceros.
- Cada flanco ascendente:
  1. `fb := r(7) xor din;`
  2. Desplaza a la izquierda: `r(6 downto 0) & '0'`.
  3. Si `fb = '1'`, XOR del resultado desplazado con el polinomio `00000111`.
- Asigna `crc <= r` de forma continua.

### Pista

Una `variable fb` dentro del proceso es la forma más limpia y sintetizable.
