## PWM — off-by-one

Le PWM doit mettre `pwm_out` à 1 tant que le compteur libre est **strictement inférieur à `duty`**.

Le starter compare à `duty - 1`, donc chaque cycle est un tick trop court. Trouve la comparaison et retire le `- 1`.

```vhdl
pwm_out <= '1' when count < unsigned(duty) else '0';
```
