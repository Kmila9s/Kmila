## Detector de flanco — flanco equivocado

Se pedía un detector de **flanco de subida** sobre `d`.

### Especificación

* Muestrea `d` una vez por `rising_edge(clk)` en `d_prev`.
* `pulse = '1'` durante un ciclo cuando `d` era `'0'` en el ciclo anterior y ahora es `'1'`.
* `reset` asíncrono activo en alto pone `d_prev` a `'0'`.

### Qué falla

La expresión combinacional de `pulse` mira la polaridad opuesta: se dispara cuando `d` baja de `'1'` a `'0'`, es decir, en el flanco de **bajada**. Cuando `d` sube (objetivo del spec) `pulse` se queda en `'0'`, y cuando `d` baja se dispara por error.

### Pista

Intercambia las dos comparaciones de la asignación condicional de `pulse`.
