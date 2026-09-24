## PWM comparator — fill the gap

⚠ 翻译未完成 — 描述以英文显示。

A 4-bit free-running counter has been wired up for you. You
must add the **comparator** that turns the counter into a
PWM signal: `pwm_out` should be **high** while `count < duty`,
and **low** otherwise.

### Hint

```vhdl
pwm_out <= '1' when count < unsigned(duty) else '0';
```

### Where to write

Only the line between `-- BEGIN_EDIT` and `-- END_EDIT` is
graded.
