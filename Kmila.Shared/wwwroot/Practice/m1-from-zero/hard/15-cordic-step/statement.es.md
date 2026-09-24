## Paso de iteración CORDIC

Implementa un paso CORDIC usando aritmética **signed de 8 bits**.

* Si `dir = '1'` (CCW): `x' = x − y`, `y' = y + x`
* Si `dir = '0'` (CW):  `x' = x + y`, `y' = y − x`

Convierte las entradas con `signed(...)` y el resultado con `std_logic_vector(...)`.

```vhdl
x_out <= std_logic_vector(signed(x_in) - signed(y_in)) when dir = '1'
    else std_logic_vector(signed(x_in) + signed(y_in));
y_out <= std_logic_vector(signed(y_in) + signed(x_in)) when dir = '1'
    else std_logic_vector(signed(y_in) - signed(x_in));
```
