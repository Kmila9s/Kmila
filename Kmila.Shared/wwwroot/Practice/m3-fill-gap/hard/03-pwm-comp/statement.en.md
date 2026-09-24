## PWM comparator — fill the gap

A 4-bit free-running counter has been wired up for you. The
counter increments every clock cycle and wraps naturally from
`1111` back to `0000` (a 16-step PWM period).

You must add the **comparator** that turns the counter into a
PWM signal: `pwm_out` should be **high** while `count < duty`,
and **low** otherwise.

### Specification

* `count` is `unsigned(3 downto 0)`.
* `duty` is `STD_LOGIC_VECTOR(3 downto 0)` — convert it with
  `unsigned(duty)`.
* Use a single conditional concurrent assignment:
  `pwm_out <= '1' when count < unsigned(duty) else '0';`
* When `duty = 0` the output stays low; when `duty = 15` it is
  high for 15 of the 16 ticks.

### Where to write

Only the line between `-- BEGIN_EDIT` and `-- END_EDIT` is
graded. Do not modify the counter process — it is locked.
