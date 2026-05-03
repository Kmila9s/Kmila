## Flip-flop T — entrada de habilitación ignorada

Se pedía un flip-flop T con **habilitación de toggle** `t`.

### Especificación

* `reset` asíncrono activo en alto pone `q` a `'0'`.
* En `rising_edge(clk)`:
  * Si `t = '1'`, alterna: `q <- not q`.
  * Si `t = '0'`, mantiene su valor.

### Qué falla

El código actual alterna `q` en cada flanco sin mirar `t`. Se comporta como un divisor de reloj, no como un flip-flop T controlable.

### Pista

La asignación de toggle debe estar dentro de un `if t = '1' then ...` dentro de la rama del flanco.
