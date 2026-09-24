## Contador de 4 bits — bug en el reset asíncrono

El bloque pretende ser un contador de 4 bits con **reset asíncrono activo en alto**.

### Especificación

* En `rising_edge(clk)`: `count <- count + 1`.
* Cuando `reset = '1'`: `count` pasa inmediatamente a `"0000"`, sin esperar al reloj.
* `q` refleja `count`.

### Qué falla

Si pulsas `reset` entre dos flancos de reloj, la cuenta **no** se borra de inmediato: solo lo hace en el siguiente flanco. El cuerpo del proceso está bien; el error está una línea más arriba.

### Pista

Para que el reset sea verdaderamente asíncrono, debe aparecer en la **lista de sensibilidad** del proceso junto al reloj.
