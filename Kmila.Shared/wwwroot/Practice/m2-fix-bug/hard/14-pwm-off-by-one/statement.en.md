## PWM — off-by-one

The PWM module should drive `pwm_out` high whenever the free-running counter is **strictly less than `duty`**.

The starter compares against `duty - 1`, so every duty cycle is one tick short. Find the comparison and remove the `- 1`.

```vhdl
pwm_out <= '1' when count < unsigned(duty) else '0';
```
