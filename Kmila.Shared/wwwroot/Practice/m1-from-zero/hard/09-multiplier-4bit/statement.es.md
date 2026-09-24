## Multiplicador 4 bits sin signo

Multiplicador combinacional: `product = a * b`, con `a` y `b` de 4 bits y `product` de 8 bits.

### Especificación

- Puro combinacional — sin reloj.
- Usa `IEEE.NUMERIC_STD`. El `*` está sobrecargado para `unsigned`.
- El ancho natural de `unsigned(3 downto 0) * unsigned(3 downto 0)` es exactamente 8 bits.

### Pista

Una línea:

```vhdl
product <= std_logic_vector(unsigned(a) * unsigned(b));
```
