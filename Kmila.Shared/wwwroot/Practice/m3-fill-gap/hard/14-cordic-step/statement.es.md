## CORDIC paso — actualización de x

Implementa una iteración CORDIC sobre la coordenada x con aritmética signed de 8 bits.

* Si `dir = '1'`: `x_out = x_in + y_in`
* Si `dir = '0'`: `x_out = x_in − y_in`

```vhdl
x_out <= std_logic_vector(signed(x_in) + signed(y_in)) when dir = '1'
    else std_logic_vector(signed(x_in) - signed(y_in));
```

Sólo se evalúa la línea entre los marcadores.
