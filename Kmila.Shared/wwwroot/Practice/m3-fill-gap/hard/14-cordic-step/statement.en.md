## CORDIC step — x update

Implement one CORDIC iteration on the x-coordinate using signed 8-bit arithmetic.

* When `dir = '1'`: `x_out = x_in + y_in`
* When `dir = '0'`: `x_out = x_in − y_in`

```vhdl
x_out <= std_logic_vector(signed(x_in) + signed(y_in)) when dir = '1'
    else std_logic_vector(signed(x_in) - signed(y_in));
```

Only the line between the markers is graded.
