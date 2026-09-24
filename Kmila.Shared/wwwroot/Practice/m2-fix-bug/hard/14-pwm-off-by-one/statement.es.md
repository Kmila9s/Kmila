## PWM — off-by-one

El PWM debe poner `pwm_out` alto siempre que el contador libre sea **estrictamente menor que `duty`**.

El starter compara contra `duty - 1`, así que cada ciclo dura un tick menos. Encuentra la comparación y quita el `- 1`.

```vhdl
pwm_out <= '1' when count < unsigned(duty) else '0';
```
