## Divisor de reloj — divisor equivocado

Se pedía un reloj dividido por 4.

### Especificación

* En cada `rising_edge(clk)` se incrementa un contador de 3 bits `cnt`.
* `clk_out` es el **bit 1** del contador, así que `clk_out` alterna cada 2 ciclos de entrada → periodo de salida = 4 ciclos → división por 4.
* `reset` asíncrono activo en alto pone `cnt` a cero.

### Qué falla

El starter conecta `cnt(2)` en lugar de `cnt(1)`. Eso divide por **8**, no por 4 — `clk_out` alterna cada 4 ciclos de entrada.

### Pista

En un divisor potencia de dos a partir de un contador libre, conectar el bit `n` divide por `2^(n+1)`. Elige el bit que dé un periodo de 4.
