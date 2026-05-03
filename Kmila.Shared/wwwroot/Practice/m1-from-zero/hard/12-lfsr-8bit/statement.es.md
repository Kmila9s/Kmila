## LFSR de 8 bits

Implementa un LFSR de 8 bits de longitud máxima con taps en 8, 6, 5, 4 (forma Fibonacci).

### Especificación

- Registro interno `r : STD_LOGIC_VECTOR(7 downto 0)`, semilla `"11111111"` en reset (cero está prohibido — se bloquearía).
- En cada flanco:
  - `fb := r(7) xor r(5) xor r(4) xor r(3);`
  - `r <= r(6 downto 0) & fb;`
- Asigna `q <= r`.

### Pista

Usa una `variable fb` dentro del proceso; mantiene la fórmula clara y sintetizable.
