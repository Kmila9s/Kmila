## Sumador completo

Implementa un sumador completo de 1 bit. Suma tres bits — `a`, `b` y un acarreo de entrada `cin` — y produce `sum` y un acarreo de salida `cout`.

### Ecuaciones

- `sum  = a XOR b XOR cin`
- `cout = (a AND b) OR (cin AND (a XOR b))`

### Pista

El XOR de las tres entradas da la suma. El acarreo de salida se genera cuando al menos dos entradas son `1`.
