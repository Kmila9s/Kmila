## Comparador PWM — completa el hueco

Tienes un contador libre de 4 bits ya cableado. Se incrementa
en cada flanco de reloj y da la vuelta de `1111` a `0000`
(periodo PWM de 16 pasos).

Tú añades el **comparador** que convierte el contador en una
señal PWM: `pwm_out` debe estar **alto** mientras
`count < duty`, y **bajo** en caso contrario.

### Especificación

* `count` es `unsigned(3 downto 0)`.
* `duty` es `STD_LOGIC_VECTOR(3 downto 0)` — conviértelo con
  `unsigned(duty)`.
* Usa una asignación condicional concurrente:
  `pwm_out <= '1' when count < unsigned(duty) else '0';`
* Con `duty = 0` la salida queda baja; con `duty = 15` queda
  alta 15 de los 16 ticks.

### Dónde escribir

Sólo la línea entre `-- BEGIN_EDIT` y `-- END_EDIT` se evalúa.
No modifiques el proceso del contador — está bloqueado.
