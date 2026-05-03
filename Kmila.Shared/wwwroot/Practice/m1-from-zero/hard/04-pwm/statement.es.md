## Generador PWM (ciclo 4 bits)

Implementa un modulador por ancho de pulso. Un contador libre `cnt` de 4 bits se incrementa cada ciclo y vuelve a 0 tras 15. La salida `pwm_out` es alta mientras `cnt < duty`.

### Especificación

- Contador `unsigned` de 4 bits que incrementa en cada flanco ascendente.
- Reset lleva el contador a 0.
- Comparación combinacional: `pwm_out = '1' when cnt < duty else '0'`.

### Pista

Usa una asignación concurrente con `when ... else` para `pwm_out` y deja que el contador corra libre.
