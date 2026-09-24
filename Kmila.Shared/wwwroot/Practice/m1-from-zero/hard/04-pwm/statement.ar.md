## مولّد PWM (4 بت)

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

## PWM generator (4-bit duty)

Implement a pulse-width modulator. A 4-bit free-running counter `cnt` increments every clock cycle and wraps to 0 after 15. The output `pwm_out` is high while `cnt < duty`.

### Spec

- 4-bit `unsigned` counter, increments on each rising edge.
- Reset clears the counter to 0.
- Combinational compare: `pwm_out = '1' when cnt < duty else '0'`.

### Hint

Use a concurrent conditional assignment for `pwm_out` and let the counter free-run inside the clocked process.
