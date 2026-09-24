## CORDIC-Iterationsschritt

Implementiere einen CORDIC-Schritt mit **8-Bit-signed**-Arithmetik.

* Bei `dir = '1'` (CCW): `x' = x − y`, `y' = y + x`
* Bei `dir = '0'` (CW):  `x' = x + y`, `y' = y − x`

Wandle Eingänge mit `signed(...)` und das Ergebnis mit `std_logic_vector(...)`.

```vhdl
x_out <= std_logic_vector(signed(x_in) - signed(y_in)) when dir = '1'
    else std_logic_vector(signed(x_in) + signed(y_in));
y_out <= std_logic_vector(signed(y_in) + signed(x_in)) when dir = '1'
    else std_logic_vector(signed(y_in) - signed(x_in));
```
