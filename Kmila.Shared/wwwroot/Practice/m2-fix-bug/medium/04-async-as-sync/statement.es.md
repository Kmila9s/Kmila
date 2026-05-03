## Flip-flop D — el reset asíncrono no lo es

Se pedía un flip-flop D con **reset asíncrono activo en alto**.

### Especificación

* En `rising_edge(clk)`: `q <- d`.
* Cuando `reset = '1'`: `q` pasa a `'0'` inmediatamente, sin esperar al reloj.

### Qué falla

Si activas `reset` entre flancos, `q` no se limpia al instante: el simulador solo re-evalúa el proceso cuando cambia `clk`. En la práctica el reset se ha vuelto **síncrono**, lo cual contradice la especificación.

### Pista

La lista de sensibilidad determina cuándo se re-evalúa un proceso. Un reset asíncrono debe figurar junto a `clk`.
