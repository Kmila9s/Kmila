## CRC-8 série

Mets à jour un registre CRC 8 bits, un bit par cycle, avec le polynôme `0x07` (`x^8 + x^2 + x + 1`).

### Spécification

- Conserve un registre `r` 8 bits, reset à zéro.
- À chaque front montant :
  1. `fb := r(7) xor din;`
  2. Décale à gauche : `r(6 downto 0) & '0'`.
  3. Si `fb = '1'`, XOR du résultat décalé avec le polynôme `00000111`.
- Assigne `crc <= r` en continu.

### Indice

Une `variable fb` dans le process est la forme la plus propre et synthétisable.
