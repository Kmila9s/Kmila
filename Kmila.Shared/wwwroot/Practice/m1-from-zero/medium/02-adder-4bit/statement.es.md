## Sumador de 4 bits

Implementa un sumador de 4 bits con acarreo de entrada `cin`. Calcula `sum = a + b + cin` y el acarreo de salida `cout`.

### Especificación

- Entradas: `a` y `b` son `STD_LOGIC_VECTOR` de 4 bits; `cin` es un bit.
- Salidas: `sum` de 4 bits; `cout` de 1 bit.

### Pista

Usa `IEEE.NUMERIC_STD.ALL` y una señal `unsigned` temporal de 5 bits:

```vhdl
signal s : unsigned(4 downto 0);
s    <= ('0' & unsigned(a)) + ('0' & unsigned(b)) + ("0000" & cin);
sum  <= std_logic_vector(s(3 downto 0));
cout <= s(4);
```
