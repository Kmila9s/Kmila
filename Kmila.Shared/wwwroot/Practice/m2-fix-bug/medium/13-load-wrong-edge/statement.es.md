## Registro con carga — flanco de reloj equivocado

Se pedía un registro de 4 bits con carga síncrona.

### Especificación

* `reset` asíncrono activo en alto pone `q` a `"0000"`.
* En `rising_edge(clk)`, si `load = '1'` entonces `q <- d`. Si no, `q` mantiene su valor.

### Qué falla

El registro se reloja en el flanco de **bajada** en lugar del de subida. Los datos llegan medio ciclo tarde y el modelo temporal está mal.

### Pista

`falling_edge(clk)` y `rising_edge(clk)` son simétricos. Mira cuál está usando el proceso.
