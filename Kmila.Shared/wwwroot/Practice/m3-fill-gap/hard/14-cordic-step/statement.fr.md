## CORDIC étape — mise à jour de x

Implémente une itération CORDIC sur la coordonnée x en arithmétique signée 8 bits.

* Si `dir = '1'` : `x_out = x_in + y_in`
* Si `dir = '0'` : `x_out = x_in − y_in`

```vhdl
x_out <= std_logic_vector(signed(x_in) + signed(y_in)) when dir = '1'
    else std_logic_vector(signed(x_in) - signed(y_in));
```

Seule la ligne entre les marqueurs est évaluée.
