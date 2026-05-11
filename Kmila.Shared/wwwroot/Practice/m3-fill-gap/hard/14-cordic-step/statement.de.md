## CORDIC-Schritt — x-Update

Implementiere eine CORDIC-Iteration für die x-Koordinate mit 8-Bit-signed-Arithmetik.

* Bei `dir = '1'`: `x_out = x_in + y_in`
* Bei `dir = '0'`: `x_out = x_in − y_in`

```vhdl
x_out <= std_logic_vector(signed(x_in) + signed(y_in)) when dir = '1'
    else std_logic_vector(signed(x_in) - signed(y_in));
```

Nur die Zeile zwischen den Markierungen wird bewertet.
