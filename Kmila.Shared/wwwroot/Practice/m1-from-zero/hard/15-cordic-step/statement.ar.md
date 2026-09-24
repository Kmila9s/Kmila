## خطوة تكرار CORDIC

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

Implement a single CORDIC rotation step using signed 8-bit arithmetic.

* When `dir = '1'` (CCW): `x' = x − y`, `y' = y + x`
* When `dir = '0'` (CW):  `x' = x + y`, `y' = y − x`

```vhdl
x_out <= std_logic_vector(signed(x_in) - signed(y_in)) when dir = '1'
    else std_logic_vector(signed(x_in) + signed(y_in));
y_out <= std_logic_vector(signed(y_in) + signed(x_in)) when dir = '1'
    else std_logic_vector(signed(y_in) - signed(x_in));
```
