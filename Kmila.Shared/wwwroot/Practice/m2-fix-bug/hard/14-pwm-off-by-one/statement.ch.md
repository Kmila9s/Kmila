## PWM — 偏一

⚠ 翻译未完成 — 描述以英文显示。

The PWM module should drive `pwm_out` high whenever the free-running counter is strictly less than `duty`. The starter compares against `duty - 1`, so every duty cycle is one tick short. Remove the `- 1`.

```vhdl
pwm_out <= '1' when count < unsigned(duty) else '0';
```
