## LFSR 8 bits

Implémente un LFSR 8 bits à longueur maximale avec taps en 8, 6, 5, 4 (forme Fibonacci).

### Spécification

- Registre interne `r : STD_LOGIC_VECTOR(7 downto 0)`, graine `"11111111"` au reset (zéro interdit — verrouillage).
- À chaque front :
  - `fb := r(7) xor r(5) xor r(4) xor r(3);`
  - `r <= r(6 downto 0) & fb;`
- `q <= r`.

### Indice

Une `variable fb` dans le process garde la formule lisible et synthétisable.
