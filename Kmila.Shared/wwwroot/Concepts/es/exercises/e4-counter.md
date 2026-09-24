**Tu objetivo:** arma un contador de 4 bits que se incremente en uno en cada flanco de subida del reloj.

Un **contador** es una máquina diminuta con estado — recuerda su valor actual entre ticks de reloj. Ese tipo de memoria solo funciona dentro de un proceso con reloj.

**Pista — prueba este diseño:**

1. Arrastra el bloque envoltorio **`on rising_edge(clk)`** al canvas.
2. Coloca el bloque **`count <= count + 1`** dentro de su hueco de cuerpo.

Eso es todo. El reproductor te mostrará el código VHDL que representa tu composición y luego recorrerá qué pasa en el primer tick del reloj (count pasa de `0000` a `0001`), el flanco de bajada (sin cambios) y el segundo flanco de subida (`0001` → `0010`).

> Ejecución secuencial dentro del cuerpo del proceso — pero el proceso mismo se **despierta** por un evento concurrente (la señal del reloj cambiando). Ambos modelos conviviendo otra vez.
