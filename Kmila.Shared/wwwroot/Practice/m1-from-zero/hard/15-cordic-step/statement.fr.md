## Étape d'itération CORDIC

Implémente une étape CORDIC avec arithmétique **signée 8 bits**.

* Si `dir = '1'` (CCW) : `x' = x − y`, `y' = y + x`
* Si `dir = '0'` (CW)  : `x' = x + y`, `y' = y − x`

Convertis les entrées avec `signed(...)` et le résultat avec `std_logic_vector(...)`.

```vhdl
x_out <= std_logic_vector(signed(x_in) - signed(y_in)) when dir = '1'
    else std_logic_vector(signed(x_in) + signed(y_in));
y_out <= std_logic_vector(signed(y_in) + signed(x_in)) when dir = '1'
    else std_logic_vector(signed(y_in) - signed(x_in));
```
